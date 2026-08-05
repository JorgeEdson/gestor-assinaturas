## Requisitos Funcionais

### Módulo de Planos

- **RF-01** — O sistema deve permitir **cadastrar um plano** com nome, valor, moeda, ciclo de cobrança (mensal/anual) e período de trial (em dias, podendo ser zero).
- **RF-02 `[DESAFIO]`** — O sistema deve permitir **editar** um plano, sem alterar retroativamente assinaturas já vinculadas.
- **RF-03 `[DESAFIO]`** — O sistema deve permitir **inativar** um plano, impedindo novas assinaturas mas preservando as existentes.

### Módulo de Clientes

- **RF-04** — O sistema deve permitir **cadastrar um cliente** com dados de identificação e contato.
- **RF-05 `[DESAFIO]`** — O sistema deve permitir consultar o **histórico de assinaturas** de um cliente.

### Módulo de Assinaturas (ciclo de vida)

- **RF-06** — O sistema deve permitir **criar uma assinatura** vinculando um cliente a um plano. Se o plano tiver trial, a assinatura inicia no status `Trial`; caso contrário, gera cobrança imediata e inicia como `Ativa`.
- **RF-07** — O sistema deve permitir **ativar uma assinatura em trial**, transicionando de `Trial` para `Ativa` e gerando a primeira fatura.
- **RF-08** — A assinatura deve **transicionar de estados de forma controlada**, respeitando as transições válidas: `Trial → Ativa`, `Ativa → Inadimplente`, `Inadimplente → Ativa`, `Inadimplente → Suspensa`, `Suspensa → Cancelada`, e `Ativa/Trial → Cancelada`. Transições inválidas devem ser rejeitadas pelo próprio domínio.
- **RF-09 `[DESAFIO]`** — O sistema deve **renovar** automaticamente a assinatura ao fim de cada ciclo, gerando nova fatura na data de vencimento.

### Módulo de Mudança de Plano

- **RF-10** — O sistema deve permitir **trocar o plano** de uma assinatura ativa (upgrade ou downgrade). A troca apenas **atualiza o valor das faturas em aberto** para o preço do novo plano, sem rateio proporcional e sem geração de créditos.

### Módulo de Cobrança e Faturas

- **RF-11** — O sistema deve **gerar faturas** com valor, moeda, data de vencimento e status (`Aberta`, `Paga`, `Falha`).
- **RF-12 `[DESAFIO]`** — O sistema deve permitir **listar faturas** por assinatura e por cliente.

### Módulo de Pagamentos e Recobrança (dunning)

- **RF-13** — O sistema deve **registrar o pagamento** de uma fatura por meio de um **gateway de pagamento externo** (abstraído por uma *port*). Pagamento aprovado marca a fatura como `Paga` e, se a assinatura estava `Inadimplente`, ela retorna ao status `Ativa`.
- **RF-14** — Pagamento **recusado** deve marcar a fatura como `Falha` e mover a assinatura para `Inadimplente`.
- **RF-15 `[DESAFIO]`** — O sistema deve executar uma política de **recobrança (dunning)**: novas tentativas em intervalos definidos; após N falhas, a assinatura é `Suspensa`.

### Módulo de Cancelamento

- **RF-16** — O sistema deve permitir **cancelar uma assinatura**, com duas modalidades: **imediato** ou **ao fim do período vigente** (mantendo acesso até o fim do ciclo já pago).

### Módulo de Notificações

- **RF-17 `[DESAFIO]`** — O sistema deve **notificar o cliente** em eventos-chave (boas-vindas, falha de pagamento, cancelamento) por meio de um **serviço de notificação abstraído por uma *port*** (e-mail, SMS ou outro canal).

### Módulo de Histórico / Auditoria

- **RF-18 `[DESAFIO]`** — O sistema deve registrar um **histórico de eventos** da assinatura (criada, ativada, plano trocado, pagamento falhou, cancelada) para fins de auditoria.

## Regras de negócio principais (invariantes do domínio)

Estas regras vivem no **núcleo** e não devem depender de banco, framework ou infraestrutura:

- **RN-01** — Uma assinatura não pode existir sem um plano válido associado.
- **RN-02** — O status da assinatura só muda por transições permitidas (RF-08); o próprio agregado é responsável por rejeitar transições inválidas.
- **RN-03** — Cálculos monetários usam sempre o objeto de valor `Money`; não se opera valor com moedas diferentes.
- **RN-04** — A troca de plano apenas atualiza o valor das faturas em aberto para o preço do novo plano; não há rateio proporcional nem crédito.
- **RN-05** — Quando o pagamento de uma fatura em aberto é confirmado, uma assinatura `Inadimplente` deve retornar a `Ativa`. Essa coordenação entre `Fatura` e `Assinatura` é responsabilidade de um **Domain Service** (`ServicoReativacao`).
- **RN-06** — Uma assinatura `Cancelada` é estado terminal: não aceita ativação, troca de plano nem novo pagamento.
- **RN-07** — Uma fatura só pode ser marcada como `Paga` mediante confirmação do gateway (via port); o domínio não conhece a implementação concreta do gateway.