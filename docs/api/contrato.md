# Contrato de API

Base path: **`/api/v1`** (versionamento por URL — ADR-0006). Formato: JSON, UTF-8.

| Serviço | Método e rota | Exigido pelo desafio |
| :--- | :--- | :---: |
| Antifraude | `POST /api/v1/transactions` | ✅ |
| Antifraude | `GET /api/v1/transactions/{id}` | ✅ |
| Antifraude | `POST /api/v1/transactions/{id}/review` | extra |
| Marketplace | `GET /api/v1/books` | extra |
| Marketplace | `POST /api/v1/purchases` | extra |
| Marketplace | `GET /api/v1/purchases/{id}` | extra |

## Convenções

- **`Idempotency-Key`** — obrigatório nos `POST` que criam recurso (`transactions`, `purchases`). Até 255
  caracteres; recomendado UUID v4. Comportamento completo no ADR-0004.
- **`X-Correlation-Id`** — opcional na requisição; se ausente, a API gera um. Sempre devolvido no header
  da resposta e propagado por todo o fluxo (ADR-0005). A correlação trafega **apenas pelo header** —
  nunca no corpo JSON; internamente fica gravada no banco.
- **Erros** — formato *Problem Details* (RFC 9457), `Content-Type: application/problem+json`.
- **Valores monetários** — `{ "value": 49.90, "currency": "BRL" }` (moeda ISO 4217).
- **Datas** — ISO 8601 em UTC (`2026-09-22T14:30:00Z`).
- **Enums** — como texto em maiúsculas com underscore (`"APPROVED"`, `"PENDING_FRAUD_CHECK"`), nunca número.
  A decisão segue literalmente o enunciado: `APPROVED`, `REJECTED`, `REVIEW`.

### Respostas comuns aos `POST` com `Idempotency-Key`

| Situação | Status |
| :--- | :--- |
| header ausente | `400 Bad Request` |
| corpo inválido | `400 Bad Request` (a chave não é registrada; pode reenviar corrigido) |
| chave nova | `202 Accepted` + `Location` |
| chave repetida, mesmo corpo | resposta original (replay) |
| chave repetida, corpo diferente | `422 Unprocessable Entity` |
| requisição original ainda em execução | `409 Conflict` |

---

## Antifraude

### `POST /api/v1/transactions`

Submete uma transação para avaliação. A decisão é assíncrona (ADR-0001).

**Headers:** `Idempotency-Key` (obrigatório), `X-Correlation-Id` (opcional).

```json
{
  "externalReference": "order-8842",
  "customerId": "cus_123",
  "amount": { "value": 189.90, "currency": "BRL" },
  "payment": { "type": "CREDIT_CARD", "fingerprint": "fp_9f2c...", "last4": "4242" },
  "channel": "WEB",
  "delivery": "DIGITAL",
  "itemCount": 1,
  "occurredAt": "2026-09-22T14:30:00Z"
}
```

| Campo | Regra |
| :--- | :--- |
| `customerId` | obrigatório |
| `amount.value` | obrigatório, maior que zero |
| `payment.fingerprint` | obrigatório — hash do meio de pagamento; nunca o número do cartão |
| `delivery` | `DIGITAL` ou `PHYSICAL` |
| `itemCount` | obrigatório, maior que zero |

**`202 Accepted`** — `Location: /api/v1/transactions/{id}`

```json
{ "transactionId": "7b1e...", "status": "RECEIVED" }
```

### `GET /api/v1/transactions/{id}`

Status e decisão da transação, com a avaliação vigente e o histórico.

**`200 OK`**

```json
{
  "transactionId": "7b1e...",
  "status": "DECIDED",
  "amount": { "value": 189.90, "currency": "BRL" },
  "receivedAt": "2026-09-22T14:30:01Z",
  "decision": {
    "outcome": "REJECTED",
    "score": 85,
    "decidedBy": { "kind": "ENGINE", "reviewerId": null },
    "decidedAt": "2026-09-22T14:30:02Z",
    "justification": null,
    "rules": [
      { "code": "HIGH_VALUE_DIGITAL", "version": 1, "hit": true, "weight": 50, "reason": "..." },
      { "code": "NEW_CUSTOMER_HIGH_AMOUNT", "version": 1, "hit": true, "weight": 35, "reason": "..." },
      { "code": "CARD_VELOCITY", "version": 1, "hit": false, "weight": 0, "reason": "..." }
    ]
  },
  "history": [ { "outcome": "REJECTED", "decidedBy": { "kind": "ENGINE" }, "decidedAt": "..." } ]
}
```

- Enquanto não há decisão, `status` é `RECEIVED` ou `PROCESSING` e `decision` é `null`.
- `history` lista todas as avaliações, da mais antiga à vigente (decisões nunca são sobrescritas).

**Erros:** `404 Not Found`.

### `POST /api/v1/transactions/{id}/review` *(extra)*

Revisão humana de uma transação em `REVIEW`. Gera uma nova avaliação e publica a decisão.

```json
{ "outcome": "APPROVED", "justification": "Cliente confirmou a compra por telefone." }
```

| Campo | Regra |
| :--- | :--- |
| `outcome` | `APPROVED` ou `REJECTED` |
| `justification` | obrigatório |

**`200 OK`** — mesmo corpo do `GET /api/v1/transactions/{id}`, já com a nova decisão.

| Erro | Quando |
| :--- | :--- |
| `400 Bad Request` | `justification` vazia ou `outcome` inválido (validação de entrada) |
| `404 Not Found` | transação inexistente |
| `409 Conflict` | a decisão vigente não é `REVIEW` (inclusive numa segunda revisão da mesma transação) |

Não usa `Idempotency-Key`: uma segunda revisão encontra a transação já decidida e recebe `409` — a
própria regra de estado a torna idempotente.

---

## Marketplace

### `GET /api/v1/books`

**`200 OK`**

```json
[
  { "bookId": "b-001", "title": "...", "author": "...", "price": { "value": 49.90, "currency": "BRL" }, "format": "PHYSICAL" },
  { "bookId": "b-002", "title": "...", "author": "...", "price": { "value": 189.90, "currency": "BRL" }, "format": "EBOOK" }
]
```

### `POST /api/v1/purchases`

Registra a intenção de compra. **Só vira venda se o antifraude aprovar.**

**Headers:** `Idempotency-Key` (obrigatório), `X-Correlation-Id` (opcional).

```json
{
  "bookId": "b-002",
  "quantity": 1,
  "customerId": "cus_123",
  "payment": { "type": "CREDIT_CARD", "fingerprint": "fp_9f2c...", "last4": "4242" },
  "channel": "WEB"
}
```

O total é calculado pelo marketplace (preço × quantidade); o formato do livro vira `delivery` ao
submeter ao antifraude.

**`202 Accepted`** — `Location: /api/v1/purchases/{id}`

```json
{ "purchaseId": "p-5521", "status": "PENDING_FRAUD_CHECK" }
```

**Erros adicionais:** `404 Not Found` (livro inexistente).

### `GET /api/v1/purchases/{id}`

**`200 OK`**

```json
{
  "purchaseId": "p-5521",
  "bookId": "b-002",
  "quantity": 1,
  "total": { "value": 189.90, "currency": "BRL" },
  "status": "CANCELLED",
  "customerMessage": "Não foi possível concluir o pagamento.",
  "fraudDetails": {
    "transactionId": "7b1e...",
    "outcome": "REJECTED",
    "score": 85,
    "rules": [ { "code": "HIGH_VALUE_DIGITAL", "hit": true, "reason": "..." } ]
  }
}
```

| `status` | `customerMessage` |
| :--- | :--- |
| `PENDING_FRAUD_CHECK` | "Pagamento em análise." |
| `CONFIRMED` | "Compra confirmada." |
| `CANCELLED` | "Não foi possível concluir o pagamento." |
| `UNDER_REVIEW` | "Pagamento em análise." |

- `customerMessage` é o texto para o comprador — nunca revela o motivo.
- `fraudDetails` é `null` enquanto não há decisão. Neste desafio fica visível para demonstrar a ligação
  entre os serviços; em produção, só para operadores de risco.

**Erros:** `404 Not Found`.
