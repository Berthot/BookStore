# ADR-0010 — Arquitetura: dois serviços HTTP + um worker, um repositório

- **Status:** aceito
- **Data:** 2026-09-24

## Contexto

O projeto nasceu como monólito modular com dois bounded contexts no mesmo processo: `BookStore` (catálogo,
compras, reconciliação) e `Fraud` (submissão, avaliação, revisão de transações). Essa decisão foi adequada
para construção rápida, mas criou dois problemas:

1. **Limitação do MassTransit 8:** o bus outbox de EF é um por bus; registrar dois
   `AddEntityFrameworkOutbox` com `UseBusOutbox()` no mesmo bus não é suportado. A saída é ter **um outbox
   por bus**.

2. **Escalabilidade e isolamento de falhas:** o Worker de avaliação de fraude e o de reconciliação de compras
   compartilhavam processo, ciclo de vida e configuração. Parar um parava o outro.

## Opções consideradas

| Opção | A favor | Contra |
| :--- | :--- | :--- |
| **Dois serviços HTTP + worker (escolhida)** | um DbContext por processo respeita o limite de um outbox por bus; isolamento real de deploy e escala | duplicação de infraestrutura HTTP (middlewares, filtros) em dois projetos |
| **Monólito com outbox por rota** | sem duplicação | não respeita o limite de um outbox por bus sem contornos no MassTransit; mantém acoplamento de deploy |
| **Micro-serviços completos com bancos separados** | máximo isolamento | exigiria migração de dados, sincronização de schemas e infra distribuída muito além do escopo do desafio |
| **Mover a outbox para o Worker** | resolve o limite mantendo uma API | concentra toda lógica de publicação de eventos no Worker, que passa a ser SPOF do fluxo de compras |

## Decisão

**Três processos, um repositório git:**

| Processo | Endpoints | Consumers | Jobs | DbContext |
| :--- | :--- | :--- | :--- | :--- |
| `BookStore.Api` | `/books`, `/purchases` | `PurchasePlaced`, `TransactionDecided` | `ReconciliationJob` | `BookStoreDbContext` |
| `Fraud.Api` | `/transactions`, `/transactions/{id}/review` | — | — | `FraudDbContext` |
| `Fraud.Worker` | — | `TransactionSubmitted`, `Fault<TransactionSubmitted>` | `IdempotencyPurgeJob` | `FraudDbContext` |

**Comunicação entre serviços:**

- `BookStore.Api` → `Fraud.Api`: HTTP síncrono via `HttpFraudCheckGateway` (typed `HttpClient` +
  `AddStandardResilienceHandler` + Aspire service discovery / `Services__fraud-api__http__0` no Compose).
  `Idempotency-Key: {purchaseId}` garante que a mesma compra nunca cria duas transações na Fraud.Api.

- `Fraud.Api` → `Fraud.Worker`: mensagem `TransactionSubmitted` via RabbitMQ outbox.

- `Fraud.Worker` → `BookStore.Api`: mensagem `TransactionDecided` via RabbitMQ, publicada pelo consumer
  (idempotência por checagem de estado).

**Cada processo tem exatamente um `DbContext` e um `AddEntityFrameworkOutbox`, o que torna `UseBusOutbox()`
seguro.**

## Consequências

**Positivas**

- `UseBusOutbox()` habilitado nos três processos, um outbox por bus.
- `BookStore.Api` e `Fraud.Api` podem ser escalados, deployados e reiniciados de forma independente.
- A chamada HTTP síncrona para a Fraud.Api carrega o `X-Correlation-Id`, mantendo rastreabilidade
  ponta a ponta no painel do Aspire/Grafana.
- A resilience handler (`AddStandardResilienceHandler`) adiciona retry, circuit breaker e timeout sem
  código próprio.

**Negativas e mitigação**

- **Latência extra:** a submissão de transação é agora HTTP em vez de in-process. Mitigação: a chamada é
  assíncrona do ponto de vista do cliente (`BookStore.Api` responde `202 Accepted` antes de completar o
  fluxo); o HTTP é local (Aspire/Compose).
- **Dependência de disponibilidade:** `BookStore.Api` aguarda `Fraud.Api` estar saudável antes de subir
  (`WaitFor` no Aspire, `depends_on: service_healthy` no Compose). Se `Fraud.Api` cair, o
  `PurchasePlacedConsumer` irá falhar e retentar via MassTransit até `Fraud.Api` voltar.
- **Duplicação de infra HTTP:** `IdempotencyFilter`, `CorrelationIdMiddleware`, `GlobalExceptionHandler`
  existem nos dois projetos de API. Mitigação: são ≤5 arquivos pequenos; extrair para uma library
  compartilhada seria prematuro para o escopo atual.

## Referências

- ADR-0002: mensageria e outbox.
- ADR-0004: idempotência.
- [MassTransit — Bus Outbox](https://masstransit.io/documentation/configuration/middleware/outbox).
