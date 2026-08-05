# Gestor Assinaturas — Aplicando a Onion Architecture na prática

> ⚠️ **Projeto didático.** Este repositório é uma **prova de conceito**, criada para **ensinar como estruturar um sistema de negócio com a Onion Architecture**: domínio rico no centro, casos de uso orquestrando sem conter regra, ports definidas nos anéis internos e infraestrutura como detalhe substituível (Result pattern, Value Objects, Domain Services, Unit of Work e regra de dependência apontando sempre para dentro). Ele **não** é uma referência pronta para produção — várias simplificações foram feitas de propósito para manter o foco no aprendizado (ver [Limitações](#limitações-por-ser-didático)).

Demonstra, ponta a ponta, como uma plataforma de **cobrança recorrente** (no estilo Stripe Billing, Chargebee, Vindi) pode ser organizada em anéis concêntricos, com o ciclo de vida da assinatura — trial, ativação, inadimplência, reativação, troca de plano e cancelamento — inteiramente protegido pelo núcleo, **testável sem banco, sem HTTP e sem framework**.

---

## Visão geral do fluxo

```
Cliente/.http → [API: Controller] → [ApplicationService] → [Domínio: Agregado / Domain Service]
                      │                      │                          │
                      │                      │              decide (Resultado: sucesso/falha)
                      │                      ▼                          │
                      │            [IUnitOfWork (port)] ◄───────────────┘
                      │                      │
                      │        implementado no anel externo
                      │                      ▼
                      │   [Infraestrutura: EF Core + SQL Server]
                      ▼
        Resultado → HTTP (201 / 200 / 204 / 404 / 422)
```

1. O **controller** recebe a requisição, monta a `Entrada` e delega ao **ApplicationService** — quatro linhas úteis, zero regra.
2. O **ApplicationService** carrega os agregados pelas ports do `IUnitOfWork`, entrega ao **domínio** e decide *quando* persistir — nunca *o que* é válido.
3. O **agregado** (`Assinatura`) e os **Domain Services** (`ServicoReativacao`, `ServicoInadimplencia`, `ServicoTrocaDePlano`) aplicam as invariantes e devolvem `Resultado` — falha de negócio é valor de retorno, não exceção.
4. A **Infraestrutura** implementa as ports: repositórios EF Core, `UnitOfWork` sobre o `DbContext`, relógio real e um **gateway de pagamento simulado**.
5. A falha volta intacta até a borda: a lista de erros acumulada pelo domínio vira o corpo do `404`/`422`.

A regra de dependência é o coração do desenho: **Dominio não referencia ninguém; Aplicacao referencia só o Dominio; Infraestrutura e Api apontam para dentro.** Trocar SQL Server por outro banco, ou a API por um console, não toca uma linha do núcleo.

---

## Padrões e mecanismos aplicados

| Padrão / mecanismo | Onde | O que resolve |
|---|---|---|
| **Onion Architecture (4 anéis)** | solução inteira | Domain Model → Application Services → Ports → Infrastructure/API; dependências sempre para dentro. |
| **Domínio isolado de frameworks** | `GestorAssinaturas.Dominio.csproj` | O `.csproj` do domínio **não tem nenhum `PackageReference`**: nem EF, nem ASP.NET. A regra de negócio não sabe onde é persistida nem como é exposta. |
| **Agregado com máquina de estados** | `Assinaturas/Assinatura.cs` | As 7 transições válidas (`Trial→Ativa`, `Ativa→Inadimplente`, `Inadimplente→Ativa`…) vivem numa única tabela (`TransicoesPermitidas`); transição inválida é rejeitada **pelo próprio agregado** (RN-02), e `Cancelada` é estado terminal (RN-06). |
| **Result pattern** | `Comum/Resultado.cs` | Falha de negócio é **valor de retorno** (`Resultado<T>`), não exceção; `Combinar` acumula **todos** os erros de validação numa única resposta. |
| **Objetos de Valor** | `ObjetosDeValor/` | `Dinheiro`, `CicloDeCobranca`, `Email` validam na construção: um objeto inválido não chega a existir. `Dinheiro` proíbe operar moedas diferentes (RN-03). |
| **Domain Services** | `ServicoReativacao` · `ServicoInadimplencia` · `ServicoTrocaDePlano` | Regra que coordena **dois agregados** (`Assinatura` + `Fatura`) e não cabe em nenhum deles: pagamento aprovado reativa inadimplente (RN-05), recusado gera inadimplência, troca de plano reprecifica só faturas em aberto (RN-04). |
| **Application Services** | `*ApplicationService.cs` + record `*Entrada` | Orquestram o caso de uso sem conter regra: carregam, delegam ao domínio, persistem e **logam a operação de negócio** sem expor dado sigiloso. |
| **Ports no anel interno** | `Aplicacao/Portas/` | `IUnitOfWork`, `IRepositorio*`, `IGatewayPagamento` (RN-07), `IRelogioDoSistema` — a Aplicação define os contratos; a Infraestrutura os implementa. |
| **Unit of Work como fachada** | `Portas/Persistencia/IUnitOfWork.cs` | Cada caso de uso depende de **uma única porta** que expõe os repositórios e o `SalvarAlteracoesAsync` (que devolve `Resultado<int>` — até falha de persistência entra no Result pattern). |
| **Gateway abstraído por port** | `IGatewayPagamento` + `GatewayPagamentoSimulado` | O domínio nunca conhece o gateway concreto (RN-07). O simulado é determinístico: valor com centavos **,99 é recusado** — o cenário de inadimplência é controlado pelo preço do plano. |
| **Relógio injetável** | `IRelogioDoSistema` | "Hoje" é dependência explícita — casos de uso testáveis com data fixa. |
| **VOs mapeados sem vazar pro domínio** | `Infraestrutura/Persistencia/Configuracoes/` | `Dinheiro` como *owned type* (`PrecoValor`/`PrecoMoeda`), `CicloDeCobranca` e `Email` por conversão, status como string legível, FK sombra `IdentificadorPlano`. |
| **EF Core Migrations no start (opcional)** | flag `AplicarMigracoesAoIniciar` | Ligada no container, desligada no dev local — mesmo caminho de evolução do schema nos dois mundos. |
| **Testes de unidade em dois anéis** | `*.Testes` | Domínio testado puro; Aplicação testada com **fakes das ports** (`UnitOfWorkEmMemoria`, `GatewayPagamentoFalso`, `RelogioFixo`) — sem banco, sem rede, sem framework. |

---

## Projetos / containers

| # | Projeto | Tipo | Responsabilidade |
|---|---|---|---|
| 1 | `GestorAssinaturas.Dominio` | Class library (.NET 10) | **Núcleo.** Entidades (`Plano`, `Cliente`, `Fatura`), agregado `Assinatura`, objetos de valor, Domain Services e o `Resultado`. **Zero dependências externas.** |
| 2 | `GestorAssinaturas.Aplicacao` | Class library (.NET 10) | Casos de uso (`*ApplicationService` + `*Entrada`) e **ports** (`IUnitOfWork`, repositórios, `IGatewayPagamento`, `IRelogioDoSistema`). Depende só do Dominio. |
| 3 | `GestorAssinaturas.Infraestrutura` | Class library (.NET 10) | EF Core 10 + SQL Server: `ContextoDeDados`, configurações de mapeamento, repositórios, `UnitOfWork`, relógio e gateway simulado. |
| 4 | `GestorAssinaturas.Api` | ASP.NET Core 10 (REST) | Controllers, tradução `Resultado` → HTTP, OpenAPI, composition root (DI) e migração no start. |
| 5 | `GestorAssinaturas.Dominio.Testes` | xUnit (.NET 10) | Invariantes dos VOs, entidades, máquina de estados e Domain Services. |
| 6 | `GestorAssinaturas.Aplicacao.Testes` | xUnit (.NET 10) | Casos de uso com fakes das ports (repos em memória, gateway falso, relógio fixo). |
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
- **RF-13/14 — Pagamentos.** Registrar pagamento via **gateway externo abstraído por port**: aprovado marca `Paga` e reativa inadimplente; recusado marca `Falha` e move para `Inadimplente`. *(RF-15 dunning: `[DESAFIO]`)*
- **RF-16 — Cancelamento.** Imediato ou **ao fim do período vigente** (agendado, mantendo o acesso até o fim do ciclo pago).
- *(RF-17 notificações e RF-18 auditoria: `[DESAFIO]`)*

> ℹ️ **Estado da API REST:** todos os casos de uso estão expostos por controller:
>
> | Caso de uso | Rota |
> |---|---|
> | Cadastrar plano | `POST api/planos` |
> | Cadastrar cliente | `POST api/clientes` |
> | Criar assinatura | `POST api/assinaturas` |
> | Ativar trial | `POST api/assinaturas/{id}/ativacao` |
> | Trocar plano | `POST api/assinaturas/{id}/troca-de-plano` |
> | Cancelar (imediato / fim do período) | `POST api/assinaturas/{id}/cancelamento` |
> | Registrar pagamento | `POST api/faturas/{id}/pagamento` |

---

## Como rodar

Pré-requisito único antes do primeiro build: gerar a migration inicial (ela é compilada no assembly da Infraestrutura):

```bash
dotnet ef migrations add CriacaoInicial -p src/gestor-assinaturas/GestorAssinaturas.Infraestrutura -s src/gestor-assinaturas/GestorAssinaturas.Api
```

Na raiz do repositório:

```bash
docker compose up --build
```

Isso sobe: **SQL Server 2022**, a **API** (que aplica as migrations no start via flag `AplicarMigracoesAoIniciar`) e o **CloudBeaver**.

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

### Rodando localmente (sem o container da API)

O `appsettings.json` aponta para `(localdb)\MSSQLLocalDB`:

```bash
dotnet ef database update -s src/gestor-assinaturas/GestorAssinaturas.Api
dotnet run --project src/gestor-assinaturas/GestorAssinaturas.Api
```

### Testes

```bash
dotnet test src/gestor-assinaturas/gestor-assinaturas.slnx
```

> ≈150 testes de unidade, todos **sem banco e sem rede** — o argumento de testabilidade da Onion na prática: as regras de transição, faturamento e reativação rodam em milissegundos com fakes das ports.

---

## Roteiro de demonstração (`.http`)

O arquivo [`src/gestor-assinaturas/GestorAssinaturas.Api/GestorAssinaturas.Api.http`](./src/gestor-assinaturas/GestorAssinaturas.Api/GestorAssinaturas.Api.http) percorre o sistema em 13 requisições, cobrindo os **dois cenários obrigatórios** da demonstração:

1. **Ativação de trial:** plano com trial → cliente → assinatura nasce `Trial` **sem fatura** → `POST /ativacao` transiciona para `Ativa` **e gera a primeira fatura** → pagamento aprovado.
2. **Reativação por pagamento:** plano de **R$ 49,99** (o gateway simulado **recusa centavos ,99**) → assinatura nasce `Ativa` com cobrança imediata → pagamento recusado marca a fatura `Falha` e move a assinatura para `Inadimplente` → uma nova fatura paga reativa a assinatura (RN-05, via `ServicoReativacao`).

Casos negativos incluídos: troca de plano em assinatura inadimplente (422), pagamento de fatura de assinatura cancelada (422, **sem acionar o gateway**), cancelamento agendado mantendo a assinatura ativa.

---

## Verificação no banco

```sql
-- Cadastros
SELECT * FROM Planos;      -- note PrecoValor / PrecoMoeda: o VO Dinheiro como owned type
SELECT * FROM Clientes;    -- Email persistido por conversão de VO

-- Ciclo de vida
SELECT Identificador, Status, DataDeInicio, DataDeTerminoDoTrial, DataDeCancelamentoAgendado, IdentificadorPlano
FROM Assinaturas;          -- Status como string legivel; IdentificadorPlano e FK sombra

-- Cobrança
SELECT Identificador, IdentificadorDaAssinatura, Valor, Moeda, DataDeVencimento, Status
FROM Faturas;

-- Assinaturas inadimplentes com fatura em aberto (candidatas a reativação)
SELECT a.Identificador, a.Status, f.Identificador AS Fatura, f.Status AS StatusDaFatura
FROM Assinaturas a
JOIN Faturas f ON f.IdentificadorDaAssinatura = a.Identificador
WHERE a.Status = 'Inadimplente' AND f.Status = 'Aberta';
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
| [`anexos/lista-requisitos-funcionais.md`](./anexos/lista-requisitos-funcionais.md) | Especificação de origem do domínio (RF-01–RF-18) e as regras de negócio invariantes (RN-01–RN-07). |

---

## Limitações (por ser didático)

Estas simplificações são **intencionais** para focar no aprendizado; em produção você trataria cada uma:

- **Segredos em texto claro** no `docker-compose.yml` e no `appsettings.json` (use secrets / variáveis de ambiente seguras).
- **API sem autenticação/autorização.**
- **Gateway de pagamento simulado e determinístico** (recusa centavos `,99`): não há integração real, retry nem webhook de confirmação assíncrona.
- **Sem consultas (GET):** a API expõe apenas comandos; listagens de planos, faturas e histórico são os `[DESAFIO]` RF-05 e RF-12.
- **Cancelamento agendado sem efetivação automática:** `EfetivarCancelamentoAgendado` existe no domínio, mas não há job que o dispare no fim do período (par natural do `[DESAFIO]` RF-09, a renovação automática).
- **Transação implícita:** os casos de uso usam apenas `SalvarAlteracoesAsync` (um `SaveChanges` atômico); `IniciarTransacaoAsync`/`ConfirmarTransacaoAsync` estão na port, mas nenhum fluxo atual exige transação explícita multi-salvamento.
- **Sem concorrência otimista:** dois pagamentos simultâneos da mesma fatura dependem da ordem de chegada; não há `rowversion`.
- **Sem notificações, histórico de eventos ou dunning** — são os `[DESAFIO]` RF-15, RF-17 e RF-18.
- **Sem métricas e sem tracing distribuído** (o `ILogger` cobre só o logging estruturado das operações de negócio).
- **Ids gerados pela aplicação** (`Guid.NewGuid()` no caso de uso), sem estratégia de geração distribuída.
