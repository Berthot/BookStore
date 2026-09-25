# Diagramas de sequência

Dois fluxos. O primeiro mostra uma transação chegando pelo `POST /api/v1/transactions` até a decisão.
O segundo mostra o marketplace de livros consumindo o antifraude via HTTP — como os três processos
cooperam numa compra de ponta a ponta.

## 1. Processamento de uma transação (Fraud.Api + Fraud.Worker)

```mermaid
sequenceDiagram
  autonumber
  actor C as Cliente
  participant FA as Fraud.Api
  participant DB as PostgreSQL (schema fraud)
  participant OB as Entrega da outbox
  participant MQ as RabbitMQ
  participant FW as Fraud.Worker

  C->>FA: POST /api/v1/transactions<br/>Idempotency-Key · X-Correlation-Id
  alt sem Idempotency-Key
    FA-->>C: 400 Bad Request
  else chave presente
    FA->>FA: SHA-256 do corpo normalizado
    FA->>DB: BEGIN · INSERT chave + hash (índice único)
    alt chave nova
      FA->>DB: INSERT Transaction (Received) + OutboxMessage (transaction-submitted) · COMMIT
      FA-->>C: 202 Accepted · Location /api/v1/transactions/{id}
    else mesma chave, mesmo hash
      FA-->>C: 202 · Idempotent-Replayed: true · Location original (replay)
    else mesma chave, hash diferente
      FA-->>C: 422 Unprocessable Entity
    else original ainda em processamento
      FA-->>C: 409 Conflict
    end
  end

  OB->>DB: lê OutboxMessage pendente
  OB->>MQ: publica transaction-submitted
  MQ->>FW: entrega a mensagem
  FW->>DB: InboxState — MessageId já processado?
  alt reentrega
    FW-->>MQ: ack (descarta, sem efeito)
  else primeira vez
    FW->>DB: Transaction → Processing · COMMIT
    FW->>DB: sinais: transações recentes do cartão, cliente novo
    FW->>FW: FraudRuleSet.Evaluate(FraudContext) → DecisionPolicy
    FW->>DB: Assessment + Transaction → Decided + OutboxMessage (transaction-decided) · COMMIT
    FW-->>MQ: ack
  end

  opt avaliação falha
    MQ->>FW: retry com backoff
    MQ->>MQ: esgotou tentativas → fila _error
    FW->>DB: fail-safe: Assessment Review (DecidedBy = System) + transaction-decided
  end

  C->>FA: GET /api/v1/transactions/{id}
  FA-->>C: 200 · status + outcome + score + regras avaliadas + histórico
```

### Pontos-chave

- **Passos 4 e 5 são atômicos.** Chave de idempotência, transação e mensagem de saída entram na mesma
  transação do banco — ou tudo, ou nada.
- **Fraud.Api não avalia nada.** Ela aceita, garante a idempotência e responde `202`. A avaliação é
  assíncrona no Fraud.Worker.
- **Replay devolve o mesmo status + Location + `Idempotent-Replayed: true`.** O cliente que recebeu `202`
  e reenvia obtém exatamente o mesmo resultado, sem reprocessar.
- **Fail-safe.** Se a avaliação esgota as tentativas, a transação recebe `Review` com
  `DecidedBy = System` e vai para análise humana.

## 2. Compra de livro de ponta a ponta (BookStore.Api → Fraud.Api → Fraud.Worker)

```mermaid
sequenceDiagram
  autonumber
  actor C as Comprador
  participant BS as BookStore.Api
  participant BDB as PostgreSQL (schema bookstore)
  participant MQ as RabbitMQ
  participant FA as Fraud.Api
  participant FDB as PostgreSQL (schema fraud)
  participant FW as Fraud.Worker

  C->>BS: GET /api/v1/books
  BS-->>C: 200 · catálogo
  C->>BS: POST /api/v1/purchases · Idempotency-Key
  BS->>BDB: Purchase (PendingFraudCheck) + OutboxMessage (purchase-placed) · COMMIT
  BS-->>C: 202 Accepted · Location /api/v1/purchases/{id}

  MQ->>BS: purchase-placed (consumer PurchasePlacedConsumer)
  BS->>FA: POST /api/v1/transactions · Idempotency-Key: PurchaseId
  FA->>FDB: Transaction (Received) + OutboxMessage (transaction-submitted) · COMMIT
  FA-->>BS: 202 Accepted

  Note over FA,FW: avaliação idêntica ao fluxo 1

  MQ->>BS: transaction-decided (consumer TransactionDecidedConsumer)
  alt Approved
    BS->>BDB: Purchase → Confirmed
  else Rejected
    BS->>BDB: Purchase → Cancelled
  else Review
    BS->>BDB: Purchase → UnderReview
  end

  C->>BS: GET /api/v1/purchases/{id}
  BS-->>C: 200 · status + customerMessage + fraudDetails

  Note over BS,BDB: Reconciliação: Purchase em PendingFraudCheck além do limite<br/>→ republica purchase-placed (seguro: chave = PurchaseId)
```

### Pontos-chave

- **Comunicação síncrona via HTTP entre BookStore.Api e Fraud.Api.** O `PurchasePlacedConsumer`
  chama `POST /api/v1/transactions` com `Idempotency-Key: PurchaseId`. Se Fraud.Api estiver fora,
  MassTransit retenta o consumer com backoff.
- **Cada transação de banco toca um schema só.** A API grava apenas em `bookstore`; a decisão final
  chega via mensagem `transaction-decided`, nunca via transação distribuída.
- **O `PurchaseId` é a chave de idempotência da transação.** Por isso a reconciliação pode republicar
  a compra quantas vezes for preciso sem criar transações duplicadas.
- **Trace ponta a ponta.** `X-Correlation-Id` atravessa HTTP e mensagens; o Aspire Dashboard mostra
  um único trace de `POST /purchases` até a atualização de `Purchase`.
