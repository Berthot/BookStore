# Diagramas de sequência

Dois fluxos. O primeiro é o exigido pelo desafio: uma transação chegando pelo contrato
`POST /api/v1/transactions` até a decisão. O segundo mostra o marketplace de livros consumindo o antifraude — como os
contextos se conectam numa compra de ponta a ponta.

## 1. Processamento de uma transação

```mermaid
sequenceDiagram
  autonumber
  actor C as Cliente
  participant API as WebApi
  participant DB as PostgreSQL (schema fraud)
  participant OB as Entrega da outbox
  participant MQ as RabbitMQ
  participant W as Worker

  C->>API: POST /api/v1/transactions<br/>Idempotency-Key · X-Correlation-Id
  alt sem Idempotency-Key
    API-->>C: 400 Bad Request
  else chave presente
    API->>API: SHA-256 do corpo normalizado
    API->>DB: BEGIN · INSERT chave + hash (índice único)
    alt chave nova
      API->>DB: INSERT Transaction (Received) + OutboxMessage (transaction-submitted) · COMMIT
      API-->>C: 202 Accepted · Location /api/v1/transactions/{id}
    else mesma chave, mesmo hash
      API-->>C: 202 com a resposta original (replay)
    else mesma chave, hash diferente
      API-->>C: 422 Unprocessable Entity
    else original ainda em processamento
      API-->>C: 409 Conflict
    end
  end

  OB->>DB: lê OutboxMessage pendente
  OB->>MQ: publica transaction-submitted
  MQ->>W: entrega a mensagem
  W->>DB: InboxState — MessageId já processado?
  alt reentrega
    W-->>MQ: ack (descarta, sem efeito)
  else primeira vez
    W->>DB: Transaction → Processing · COMMIT
    W->>DB: sinais: transações recentes do cartão, cliente novo
    W->>W: FraudRuleSet.Evaluate(FraudContext) → DecisionPolicy
    W->>DB: Assessment + Transaction → Decided + OutboxMessage (transaction-decided) · COMMIT
    W-->>MQ: ack
  end

  opt avaliação falha
    MQ->>W: retry com backoff
    MQ->>MQ: esgotou tentativas → fila _error
    W->>DB: fail-safe: Assessment Review (DecidedBy = System) + transaction-decided
  end

  C->>API: GET /api/v1/transactions/{id}
  API-->>C: 200 · status + outcome + regras avaliadas
```

### Pontos-chave

- **Passos 4 e 5 são atômicos.** Chave de idempotência, transação e mensagem de saída entram na mesma
  transação do banco — ou tudo, ou nada. Não existe transação sem mensagem nem mensagem sem transação.
- **A API não avalia nada.** Ela aceita, garante a idempotência e responde `202`. A avaliação é
  assíncrona, no Worker; o cliente acompanha pelo `GET` (passo 23).
- **Duas camadas de deduplicação.** Na entrada, a `Idempotency-Key` barra reenvios do cliente. No
  consumo, a inbox barra reentregas do broker — a outbox garante entrega *pelo menos uma vez*, então
  a mesma mensagem pode chegar duas vezes.
- **`Processing` é gravado antes de avaliar.** O `GET` mostra que a transação está em andamento, e uma
  falha no meio da avaliação deixa rastro no estado.
- **Fail-safe.** Se a avaliação esgota as tentativas, a transação não fica sem decisão nem é aprovada
  às cegas: recebe `Review` com `DecidedBy = System` e vai para análise humana.

## 2. Compra de livro de ponta a ponta

```mermaid
sequenceDiagram
  autonumber
  actor C as Comprador
  participant API as WebApi
  participant BS as PostgreSQL (schema bookstore)
  participant MQ as RabbitMQ
  participant W as Worker
  participant FR as PostgreSQL (schema fraud)

  C->>API: GET /api/v1/books
  API-->>C: 200 · catálogo
  C->>API: POST /api/v1/purchases · Idempotency-Key
  API->>BS: Purchase (PendingFraudCheck) + OutboxMessage (purchase-placed) · COMMIT
  API-->>C: 202 Accepted · Location /api/v1/purchases/{id}

  MQ->>W: purchase-placed
  W->>W: IFraudCheckGateway.Submit(Idempotency-Key = PurchaseId, Delivery, ItemCount)
  W->>FR: SubmitTransaction — mesmo caso de uso do fluxo 1
  Note over W,FR: avaliação idêntica ao fluxo 1 → transaction-decided

  MQ->>W: transaction-decided
  alt Approved
    W->>BS: Purchase → Confirmed (a venda acontece)
  else Rejected
    W->>BS: Purchase → Cancelled
  else Review
    W->>BS: Purchase → UnderReview
  end

  C->>API: GET /api/v1/purchases/{id}
  API-->>C: 200 · status + customerMessage + fraudDetails

  Note over W,BS: Reconciliação: Purchase em PendingFraudCheck além do limite<br/>→ republica purchase-placed (seguro: chave = PurchaseId)
```

### Pontos-chave

- **Cada transação de banco toca um schema só.** A API grava apenas em `bookstore`; o Worker grava em
  `fraud` ao submeter e em `bookstore` ao receber a decisão. A ponte entre os contextos é sempre uma
  mensagem, nunca uma transação distribuída.
- **O `PurchaseId` é a chave de idempotência da transação.** Por isso a reconciliação pode republicar
  a compra quantas vezes for preciso: se a transação já existe, o antifraude devolve a mesma.
- **Só existe venda com `Approved`.** `Confirmed` só é alcançado por um `transaction-decided` aprovado —
  do motor ou de um revisor.
- **Dois públicos, campos separados.** O comprador lê `customerMessage` (genérica); `fraudDetails`
  mostra as regras e os motivos. Em produção, `fraudDetails` ficaria restrito a operadores de risco.
