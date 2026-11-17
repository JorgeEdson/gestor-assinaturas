# Gestor Assinaturas — Aplicando a Onion Architecture na prática

> ⚠️ **Projeto didático.** Este repositório é uma **prova de conceito**, criada para **ensinar como estruturar um sistema de negócio com a Onion Architecture**: modelo de domínio rico no centro, serviços de domínio e contratos de repositório no anel seguinte, casos de uso orquestrando sem conter regra e infraestrutura como detalhe substituível — com a regra de dependência apontando sempre para dentro. Ele **não** é uma referência pronta para produção — várias simplificações foram feitas de propósito para manter o foco no aprendizado (ver [Limitações](#limitações-por-ser-didático)).

Demonstra, ponta a ponta, como uma plataforma de **cobrança recorrente** (no estilo Stripe Billing, Chargebee, Vindi) pode ser organizada em anéis concêntricos, com o ciclo de vida da assinatura — trial, ativação, inadimplência, recobrança, reativação, troca de plano e cancelamento — inteiramente protegido pelo núcleo, **testável sem banco, sem HTTP e sem framework**.

---

## Os anéis da Onion neste projeto

A Onion Architecture (Jeffrey Palermo, 2008) organiza o sistema em anéis. Cada anel só conhece os anéis **mais internos**; quem está fora depende de quem está dentro, nunca o contrário.

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Anel externo — Interface do usuário, Infraestrutura e Testes             │
│  GestorAssinaturas.Api · GestorAssinaturas.Infraestrutura · *.Testes      │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │  Serviços de Aplicação                                              │  │
│  │  GestorAssinaturas.Aplicacao  (*ServicoDeAplicacao, Portas/)        │  │
│  │  ┌───────────────────────────────────────────────────────────────┐  │  │
│  │  │  Serviços de Domínio                                          │  │  │
│  │  │  Dominio/Servicos  ·  Dominio/Repositorios (contratos)        │  │  │
│  │  │  ┌─────────────────────────────────────────────────────────┐  │  │  │
│  │  │  │  Modelo de Domínio                                      │  │  │  │
│  │  │  │  Assinaturas · Planos · Clientes · Faturas ·            │  │  │  │
│  │  │  │  ObjetosDeValor · Comum                                 │  │  │  │
│  │  │  └─────────────────────────────────────────────────────────┘  │  │  │
│  │  └───────────────────────────────────────────────────────────────┘  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

| Anel | Onde está | O que contém | Depende de |
|---|---|---|---|
| **1. Modelo de Domínio** | `Dominio/Assinaturas`, `Planos`, `Clientes`, `Faturas`, `ObjetosDeValor`, `Comum` | Agregados, entidades, objetos de valor, máquina de estados e o `Resultado`. | ninguém |
| **2. Serviços de Domínio** | `Dominio/Servicos`, `Dominio/Repositorios` | Regras que coordenam mais de um agregado (`ServicoReativacao`, `ServicoInadimplencia`, `ServicoTrocaDePlano`, `ServicoRecobranca`) e os **contratos de persistência** (`IRepositorio*`, `IUnidadeDeTrabalho`). | anel 1 |
| **3. Serviços de Aplicação** | `GestorAssinaturas.Aplicacao` | Casos de uso (`*ServicoDeAplicacao` + `*Entrada`) e as **ports de aplicação** que não são do negócio: `IGatewayPagamento` e `IRelogioDoSistema`. | anéis 1 e 2 |
| **4. Anel externo** | `GestorAssinaturas.Infraestrutura`, `GestorAssinaturas.Api`, `*.Testes` | EF Core, SQL Server, gateway simulado, relógio real, carga inicial, controllers HTTP, composition root e testes. | anéis internos |

Os anéis 1 e 2 moram no mesmo projeto (`GestorAssinaturas.Dominio`), separados por pasta e namespace. Isso garante, pelo compilador, que **nenhum dos dois** pode referenciar EF, ASP.NET ou a Aplicação: o `.csproj` do domínio não tem nenhum `PackageReference`.

> **Por que os contratos de repositório ficam no Domínio e não na Aplicação?** No modelo original da Onion, as interfaces de repositório pertencem ao núcleo — é o núcleo que diz *o que* precisa ser persistido, e a infraestrutura obedece. Já o gateway de pagamento e o relógio são necessidades **do caso de uso**, não do modelo de negócio; por isso ficam como ports no anel de Aplicação. Essa é a principal diferença prática em relação à Clean Architecture e à Arquitetura Hexagonal, que colocam todas as ports na camada de casos de uso.

---

## Visão geral do fluxo

```
Cliente/.http → [Api: Controller] → [ServicoDeAplicacao] → [Domínio: Agregado / Serviço de Domínio]
                      │                      │                          │
                      │                      │              decide (Resultado: sucesso/falha)
                      │                      ▼                          │
                      │        [IUnidadeDeTrabalho (contrato do Domínio)] ◄┘
                      │                      │
                      │        implementado no anel externo
                      │                      ▼
                      │   [Infraestrutura: EF Core + SQL Server]
                      ▼
        Resultado → HTTP (201 / 200 / 204 / 404 / 422)   ·   exceção técnica → 500 (ProblemDetails)
```

1. O **controller** recebe a requisição, monta a `Entrada` e delega ao **serviço de aplicação** — zero regra.
2. O **serviço de aplicação** carrega os agregados pelos repositórios da `IUnidadeDeTrabalho`, entrega ao **domínio** e decide *quando* persistir — nunca *o que* é válido. Até perguntas como "esta fatura pode ser cobrada?" são respondidas pelo agregado (`Assinatura.VerificarSePodeReceberPagamentoDa`).
3. O **agregado** (`Assinatura`) e os **Serviços de Domínio** aplicam as invariantes e devolvem `Resultado` — falha de negócio é valor de retorno, não exceção.
4. A **Infraestrutura** implementa os contratos: repositórios EF Core, `UnidadeDeTrabalho` sobre o `ContextoDeDados`, relógio real e um **gateway de pagamento simulado**.
5. A falha volta intacta até a borda com o seu **tipo** (`TipoDeFalha`): `RecursoNaoEncontrado` vira `404`, `ViolacaoDeRegraDeNegocio` vira `422`. Falhas técnicas (banco fora do ar, por exemplo) são **exceções**, registradas em log pelo `ManipuladorDeExcecoesNaoTratadas` e devolvidas como `500` sem expor detalhes internos.

---

## Padrões e mecanismos aplicados

| Padrão / mecanismo | Onde | O que resolve |
|---|---|---|
| **Onion Architecture (4 anéis)** | solução inteira | Modelo de Domínio → Serviços de Domínio → Serviços de Aplicação → Interface/Infraestrutura/Testes; dependências sempre para dentro. |
| **Domínio isolado de frameworks** | `GestorAssinaturas.Dominio.csproj` | Nenhum `PackageReference`: nem EF, nem ASP.NET. A regra de negócio não sabe onde é persistida nem como é exposta. |
| **Agregado com máquina de estados** | `Assinaturas/Assinatura.cs` | As 7 transições válidas vivem numa única tabela (`TransicoesPermitidas`); transição inválida é rejeitada **pelo próprio agregado** (RN-02), e `Cancelada` é estado terminal (RN-06). |
| **Referência entre agregados por identidade** | `Assinatura.IdentificadorDoPlano` | A assinatura guarda o identificador do plano e uma cópia do **preço e do ciclo contratados** (`ValorDaAssinatura`, `CicloDeCobranca`) — sem navegação para outro agregado (RN-09). |
| **Regra temporal no domínio** | `Assinatura.CalcularDataDeFimDoPeriodoVigente` · `Assinatura.Cancelar` | O fim do período vigente e a escolha da modalidade de cancelamento são decididos pelo agregado; o caso de uso só informa "hoje" (RN-10). |
| **Result pattern com tipo de falha** | `Comum/Resultado.cs` · `Comum/TipoDeFalha.cs` | Falha de negócio é **valor de retorno**; `Combinar` acumula **todos** os erros; `FalhaAPartirDe` propaga a falha **preservando o tipo**, e a Api decide o status HTTP pelo tipo, não pelo texto da mensagem. |
| **Objetos de Valor** | `ObjetosDeValor/` | `Dinheiro`, `CicloDeCobranca`, `Email` validam na construção: um objeto inválido não chega a existir. `Dinheiro` proíbe operar moedas diferentes (RN-03). |
| **Serviços de Domínio** | `Dominio/Servicos/` | Regras que coordenam **dois agregados** (`Assinatura` + `Fatura`): pagamento aprovado reativa inadimplente (RN-05), recusado gera inadimplência, troca de plano reprecifica só faturas em aberto (RN-04), recobrança só sem fatura em aberto (RN-08). |
| **Contratos de repositório no núcleo** | `Dominio/Repositorios/` | `IRepositorio*` e `IUnidadeDeTrabalho` são definidos pelo domínio e implementados pela Infraestrutura. Buscar um registro inexistente devolve `null` — "não encontrado" é decidido pelo caso de uso. |
| **Serviços de Aplicação** | `*ServicoDeAplicacao.cs` + record `*Entrada` | Orquestram o caso de uso sem conter regra: carregam, delegam ao domínio, persistem e **registram em log a operação de negócio** sem expor dado pessoal (nome e e-mail do cliente não vão para o log). |
| **Ports de aplicação** | `Aplicacao/Portas/` | `IGatewayPagamento` (RN-07) e `IRelogioDoSistema`: necessidades do caso de uso que a Infraestrutura implementa. |
| **Unidade de Trabalho** | `IUnidadeDeTrabalho` | Cada caso de uso depende de **um único contrato** que expõe os repositórios e o `SalvarAlteracoesAsync` (um `SaveChanges` atômico). |
| **Gateway abstraído por port** | `IGatewayPagamento` + `GatewayPagamentoSimulado` | O domínio nunca conhece o gateway concreto (RN-07). O simulado é determinístico: a **primeira** cobrança de cada assinatura com centavos **,99** é recusada; as seguintes são aprovadas (simula um cartão regularizado pelo cliente). |
| **Relógio injetável** | `IRelogioDoSistema` | "Hoje" é dependência explícita — casos de uso testáveis com data fixa. |
| **Tratamento de falhas técnicas na borda** | `Api/Configuracao/ManipuladorDeExcecoesNaoTratadas.cs` | Exceções de infraestrutura não viram `Resultado`: são registradas em log e devolvidas como `500` (ProblemDetails) sem vazar mensagens internas. |
| **VOs mapeados sem vazar pro domínio** | `Infraestrutura/Persistencia/Configuracoes/` | `Dinheiro` como *owned type*, `CicloDeCobranca` e `Email` por conversão, status como string legível, FK `IdentificadorDoPlano` sem navegação. |
| **EF Core Migrations e carga inicial na Infraestrutura** | `AplicarMigracoesDoBancoDeDadosAsync` · `CargaInicialDoBancoDeDados` | A Api só chama extensões da Infraestrutura; ela não referencia o EF Core. |
| **Testes de unidade em dois anéis** | `*.Testes` | Domínio testado puro; Aplicação testada com **dublês** (`UnidadeDeTrabalhoEmMemoria`, `GatewayPagamentoFalso`, `RelogioFixo`) — sem banco, sem rede, sem framework. |

---

## Projetos / containers

| # | Projeto | Tipo | Responsabilidade |
|---|---|---|---|
| 1 | `GestorAssinaturas.Dominio` | Class library (.NET 10) | **Anéis 1 e 2.** Modelo de domínio (`Plano`, `Cliente`, `Fatura`, agregado `Assinatura`, objetos de valor, `Resultado`), Serviços de Domínio e contratos de repositório. **Zero dependências externas.** |
| 2 | `GestorAssinaturas.Aplicacao` | Class library (.NET 10) | **Anel 3.** Casos de uso (`*ServicoDeAplicacao` + `*Entrada`) e ports de aplicação (`IGatewayPagamento`, `IRelogioDoSistema`). Depende só do Dominio. |
| 3 | `GestorAssinaturas.Infraestrutura` | Class library (.NET 10) | **Anel externo.** EF Core 10 + SQL Server: `ContextoDeDados`, mapeamentos, repositórios, `UnidadeDeTrabalho`, migrations, carga inicial, relógio e gateway simulado. |
| 4 | `GestorAssinaturas.Api` | ASP.NET Core 10 (REST) | **Anel externo.** Controllers, tradução `Resultado` → HTTP, tratamento de exceções, OpenAPI e composition root (DI). |
| 5 | `GestorAssinaturas.Dominio.Testes` | xUnit (.NET 10) | Invariantes dos VOs, entidades, máquina de estados, cálculo de período e Serviços de Domínio. |
| 6 | `GestorAssinaturas.Aplicacao.Testes` | xUnit (.NET 10) | Casos de uso com dublês dos contratos (repositórios em memória, gateway falso, relógio fixo). |
| — | SQL Server 2022 | Infra (container) | Banco `GestorAssinaturas`. |
| — | CloudBeaver | Infra (container) | Inspeção do banco pelo navegador. |

---

## Requisitos funcionais (RF)

> Especificação completa em [`anexos/lista-requisitos-funcionais.md`](./anexos/lista-requisitos-funcionais.md). Os itens marcados com `[DESAFIO]` **não estão implementados** — ficam propostos como exercício de extensão, mantendo o mesmo padrão arquitetural.

- **RF-01 — Planos.** Cadastrar plano com nome, valor, moeda, ciclo (mensal/anual) e trial em dias. *(RF-02 editar e RF-03 inativar: `[DESAFIO]`)*
- **RF-04 — Clientes.** Cadastrar cliente com identificação e contato (e-mail validado por VO). *(RF-05 histórico: `[DESAFIO]`)*
- **RF-06/07/08 — Assinaturas.** Criar assinatura (nasce `Trial` ou `Ativa` com cobrança imediata), ativar trial gerando a primeira fatura, e transicionar de estados **de forma controlada pelo agregado**. *(RF-09 renovação automática: `[DESAFIO]`)*
- **RF-10 — Troca de plano.** Upgrade/downgrade de assinatura ativa, atualizando **só as faturas em aberto** para o novo preço — sem rateio, sem crédito.
- **RF-11 — Faturas.** Emitidas com valor, moeda, vencimento e status `Aberta`/`Paga`/`Falha`. *(RF-12 listagens: `[DESAFIO]`)*
- **RF-13/14 — Pagamentos.** Registrar pagamento via **gateway externo abstraído por port**: aprovado marca `Paga` e reativa inadimplente; recusado marca `Falha` e move para `Inadimplente`.
- **RF-15 — Recobrança.** Emitir uma nova fatura para uma assinatura `Inadimplente` quando não houver fatura em aberto. *(Política automática de dunning com suspensão após N falhas: `[DESAFIO]`)*
- **RF-16 — Cancelamento.** Imediato ou **ao fim do período vigente** (agendado pelo domínio, mantendo o acesso até o fim do ciclo pago).
- *(RF-17 notificações e RF-18 auditoria: `[DESAFIO]`)*

> ℹ️ **Estado da API REST:** todos os casos de uso estão expostos por controller:
>
> | Caso de uso | Rota |
> |---|---|
> | Cadastrar plano | `POST api/planos` |
> | Cadastrar cliente | `POST api/clientes` |
> | Criar assinatura | `POST api/assinaturas` |
> | Ativar trial | `POST api/assinaturas/{id}/ativacao` |
> | Emitir recobrança de inadimplente | `POST api/assinaturas/{id}/recobranca` |
> | Trocar plano | `POST api/assinaturas/{id}/troca-de-plano` |
> | Cancelar (imediato / fim do período) | `POST api/assinaturas/{id}/cancelamento` |
> | Registrar pagamento | `POST api/faturas/{id}/pagamento` |

---

## Como rodar

Na raiz do repositório:

```bash
docker compose up --build
```

Isso sobe: **SQL Server 2022**, a **API** (que aplica as migrations no start via flag `AplicarMigracoesAoIniciar` e a carga inicial em `Development`) e o **CloudBeaver**.

Endereços padrão:

| Serviço | URL / Porta | Credenciais |
|---|---|---|
| API | http://localhost:5080 | — |
| OpenAPI (Development) | http://localhost:5080/openapi/v1.json | — |
| CloudBeaver | http://localhost:8978 | cria o admin no primeiro acesso |
| SQL Server | `localhost:1433` | `sa` / `SenhaForte!2026` (db `GestorAssinaturas`) |

No CloudBeaver, registre a conexão: driver **SQL Server**, host `sqlserver`, porta `1433`, database `GestorAssinaturas`, usuário `sa`.

Para parar:

```bash
docker compose down
```

> Os volumes `dados-sqlserver` e `dados-cloudbeaver` **persistem** entre reinícios. Para começar do zero: `docker compose down -v`.

### Migrations

As migrations ficam na Infraestrutura, que também é o projeto de inicialização em tempo de design (`FabricaDoContextoDeDadosEmTempoDeDesign`). Para criar uma nova migration depois de alterar o mapeamento:

```bash
dotnet ef migrations add NomeDaMigration -p src/gestor-assinaturas/GestorAssinaturas.Infraestrutura -s src/gestor-assinaturas/GestorAssinaturas.Infraestrutura
```

### Rodando localmente (sem o container da API)

O `appsettings.json` aponta para `(localdb)\MSSQLLocalDB`:

```bash
dotnet ef database update -p src/gestor-assinaturas/GestorAssinaturas.Infraestrutura -s src/gestor-assinaturas/GestorAssinaturas.Infraestrutura
dotnet run --project src/gestor-assinaturas/GestorAssinaturas.Api
```

### Testes

```bash
dotnet test src/gestor-assinaturas/gestor-assinaturas.slnx
```

> ≈190 testes de unidade, todos **sem banco e sem rede** — o argumento de testabilidade da Onion na prática: as regras de transição, faturamento, recobrança, reativação e cálculo de período rodam em milissegundos com dublês dos contratos.

---

## Roteiro de demonstração (`.http`)

O arquivo [`src/gestor-assinaturas/GestorAssinaturas.Api/GestorAssinaturas.Api.http`](./src/gestor-assinaturas/GestorAssinaturas.Api/GestorAssinaturas.Api.http) percorre o sistema em 17 requisições (a collection do Insomnia em `anexos/` faz o mesmo de forma encadeada), cobrindo os **dois cenários obrigatórios** da demonstração:

1. **Ativação de trial:** plano com trial → assinatura nasce `Trial` **sem fatura** → `POST /ativacao` transiciona para `Ativa` **e gera a primeira fatura** → pagamento aprovado.
2. **Reativação por pagamento:** plano de **R$ 49,99** → assinatura nasce `Ativa` com cobrança imediata → o gateway simulado **recusa a primeira cobrança ,99**: fatura `Falha` e assinatura `Inadimplente` → `POST /recobranca` emite uma nova fatura em aberto (`ServicoRecobranca`) → o pagamento é aprovado e o `ServicoReativacao` devolve a assinatura para `Ativa` (RN-05).

Casos negativos incluídos: troca de plano em assinatura inadimplente (422), segunda recobrança com fatura em aberto (422), recobrança de assinatura inexistente (404, decidido pelo `TipoDeFalha`), pagamento de fatura de assinatura cancelada (422, **sem acionar o gateway**), cancelamento agendado mantendo a assinatura ativa.

---

## Verificação no banco

```sql
-- Cadastros
SELECT * FROM Planos;      -- note PrecoValor / PrecoMoeda: o VO Dinheiro como owned type
SELECT * FROM Clientes;    -- Email persistido por conversão de VO

-- Ciclo de vida
SELECT Identificador, Status, IdentificadorDoPlano, ValorDaAssinatura, MoedaDaAssinatura, CicloDeCobranca,
       DataDeInicio, DataDeTerminoDoTrial, DataDeInicioDaCobranca, DataDeCancelamentoAgendado
FROM Assinaturas;          -- preço e ciclo contratados ficam na assinatura; o plano é referenciado só pelo identificador

-- Cobrança
SELECT Identificador, IdentificadorDaAssinatura, Valor, Moeda, DataDeVencimento, Status
FROM Faturas;

-- Histórico de cobrança da assinatura do cenário 2: Falha seguida de Paga
SELECT a.Identificador, a.Status, f.Identificador AS Fatura, f.Status AS StatusDaFatura, f.DataDeVencimento
FROM Assinaturas a
JOIN Faturas f ON f.IdentificadorDaAssinatura = a.Identificador
WHERE a.IdentificadorDoPlano = '33333333-3333-3333-3333-333333333333'
ORDER BY a.Identificador, f.DataDeVencimento;
```

---

## Tecnologias

**Base:** .NET 10 · C# · ASP.NET Core (controllers) · OpenAPI
**Persistência:** SQL Server 2022 · EF Core 10 · EF Migrations
**Testes:** xUnit
**Infra local:** Docker Compose · CloudBeaver

> Note o que **não** está aqui: nenhuma biblioteca de mediator, de mapeamento, de validação ou de Result. O `Resultado`, a validação nos objetos de valor e a máquina de estados são do próprio projeto — de propósito, para que o mecanismo fique visível em vez de escondido atrás de um pacote.

---

## Documentação complementar

| Documento | Conteúdo |
|---|---|
| [`anexos/lista-requisitos-funcionais.md`](./anexos/lista-requisitos-funcionais.md) | Especificação de origem do domínio (RF-01–RF-18) e as regras de negócio invariantes (RN-01–RN-10). |
| [`anexos/gestor-assinaturas-fluxo-demo.insomnia.json`](./anexos/gestor-assinaturas-fluxo-demo.insomnia.json) | Collection do Insomnia com os cenários encadeados. |

---

## Limitações (por ser didático)

Estas simplificações são **intencionais** para focar no aprendizado; em produção você trataria cada uma:

- **Segredos em texto claro** no `docker-compose.yml` e no `appsettings.json` (use secrets / variáveis de ambiente seguras).
- **API sem autenticação/autorização.**
- **Gateway de pagamento simulado:** a regra da "primeira cobrança ,99 recusada" fica em memória e é reiniciada junto com a API; não há integração real, retry nem webhook de confirmação assíncrona.
- **Cobrança antes da persistência:** o gateway é acionado antes do `SalvarAlteracoesAsync`. Se a gravação falhar, a cobrança já foi feita e a fatura continua `Aberta`. Em produção isso pede idempotência no gateway (chave por fatura) e/ou o padrão *outbox*.
- **Sem consultas (GET):** a API expõe apenas comandos; listagens de planos, faturas e histórico são os `[DESAFIO]` RF-05 e RF-12.
- **Sem jobs agendados:** `EfetivarCancelamentoAgendado` e `Suspender` existem no domínio e estão testados, mas nada os dispara automaticamente — são o par natural do `[DESAFIO]` RF-09 (renovação) e da política automática de dunning (RF-15).
- **Sem concorrência otimista:** dois pagamentos simultâneos da mesma fatura dependem da ordem de chegada; não há `rowversion`.
- **Sem notificações e sem histórico de eventos** — são os `[DESAFIO]` RF-17 e RF-18.
- **Sem métricas e sem tracing distribuído** (o `ILogger` cobre só o logging estruturado das operações de negócio).
- **Identificadores gerados pela aplicação** (`Guid.NewGuid()` no caso de uso), sem estratégia de geração distribuída.
- **Enums do domínio no contrato HTTP** (`TipoDeCicloDeCobranca`, `ModalidadeDeCancelamento`): o anel externo pode depender dos internos, então é permitido; uma API pública teria contratos próprios para não acoplar a versão do domínio à do cliente.
