# Demo — BookStore Antifraude

Roteiro completo de demonstração da plataforma. Todos os comandos assumem o diretório raiz do repositório.

---

## Pré-requisitos

| Ferramenta | Versão mínima | Verificar |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` |
| Docker Desktop | 4.x | `docker --version` |
| Newman | 6.x | `newman --version` |

```bash
# Instalar Newman globalmente (caso não tenha)
npm install -g newman
```

---

## 1. Subir a stack com Aspire (recomendado)

```bash
dotnet run --project apps/Aspire
```

O Aspire Dashboard abre automaticamente em **http://localhost:15270**. Aguarde todos os recursos ficarem `Running` (≈ 30 s).

### Portas dos serviços

| Serviço | URL |
|---|---|
| Aspire Dashboard | http://localhost:15270 |
| BookStore API | ver coluna Endpoints no Dashboard → `bookstore-api` |
| Fraud API | ver coluna Endpoints no Dashboard → `fraud-api` |
| Grafana | http://localhost:3000 (user: `admin` / senha: `bookstore`) |
| Prometheus | http://localhost:9090 |
| RabbitMQ Management | http://localhost:15672 (user: `guest` / senha: veja parâmetros Aspire) |

> **Dica Postman/Newman:** Abra `docs/postman/local-aspire.postman_environment.json`, atualize
> `bookstoreUrl` e `fraudUrl` com as portas exibidas no Dashboard e salve.

---

## 2. Subir sem Aspire (alternativo)

Requer PostgreSQL na porta 5432 e RabbitMQ na porta 5672 rodando localmente.

```bash
dotnet run --project apps/BookStore.Api   # http://localhost:8080
dotnet run --project apps/Fraud.Api       # http://localhost:8081
dotnet run --project apps/Fraud.Worker
```

Ambiente Postman: `docs/postman/local.postman_environment.json`

---

## 3. Swagger

| API | URL |
|---|---|
| BookStore API | `{bookstoreUrl}/swagger` |
| Fraud API | `{fraudUrl}/swagger` |

---

## 4. Fluxo de compra — passo a passo

### 4.1 Criar uma compra (aprovação automática)

```http
POST {bookstoreUrl}/api/v1/purchases
Idempotency-Key: demo-001
Content-Type: application/json

{
  "bookId": "11111111-0000-0000-0000-000000000001",
  "quantity": 1,
  "customerId": "customer-demo",
  "paymentType": "CreditCard",
  "paymentFingerprint": "card-0001",
  "paymentLast4": "1234"
}
```

Anote o `purchaseId` retornado.

### 4.2 Acompanhar o status

```http
GET {bookstoreUrl}/api/v1/purchases/{purchaseId}
```

Sequência esperada: `PendingFraudCheck` → `Confirmed` (em ≈ 35 s com delay de demo).

Durante o delay, a compra aparece em `PendingFraudCheck` e a transação no Fraud.Api como `Processing`.

### 4.3 Ver a decisão de fraude

```http
GET {fraudUrl}/api/v1/transactions/{transactionId}
```

O campo `decision` mostra `outcome`, `score`, `decidedBy` e as `rules` ativadas.

### 4.4 Compra rejeitada (valor alto + ebook)

```http
POST {bookstoreUrl}/api/v1/purchases
Idempotency-Key: demo-002
Content-Type: application/json

{
  "bookId": "11111111-0000-0000-0000-000000000002",
  "quantity": 1,
  "customerId": "customer-novo",
  "paymentType": "CreditCard",
  "paymentFingerprint": "card-0002",
  "paymentLast4": "5678"
}
```

Resultado esperado: `Cancelled` com `outcome=Rejected` (regra `HIGH_VALUE_DIGITAL` ativa).

### 4.5 Compra para revisão humana

Use um valor entre R$315 e R$349,90 (structuring band) ou simule novo cliente com valor elevado.

```http
POST {bookstoreUrl}/api/v1/purchases
Idempotency-Key: demo-003
Content-Type: application/json

{
  "bookId": "11111111-0000-0000-0000-000000000003",
  "quantity": 2,
  "customerId": "customer-review",
  "paymentType": "CreditCard",
  "paymentFingerprint": "card-0003",
  "paymentLast4": "9999"
}
```

### 4.6 Revisar manualmente

```http
POST {fraudUrl}/api/v1/transactions/{transactionId}/review
Idempotency-Key: review-001
Content-Type: application/json

{
  "outcome": "APPROVED",
  "justification": "Verificado com o cliente por telefone."
}
```

---

## 5. Newman — cenários S1–S11

```bash
# Aspire (ajuste a porta conforme o Dashboard)
newman run docs/postman/BookStore.postman_collection.json \
  -e docs/postman/local-aspire.postman_environment.json \
  --delay-request 500

# Standalone (portas fixas 8080/8081)
newman run docs/postman/BookStore.postman_collection.json \
  -e docs/postman/local.postman_environment.json \
  --delay-request 500
```

Aguarde ≈ 40 s entre o envio de cada cenário que aciona o Fraud.Worker (o delay de demo é 30 s).

---

## 6. Teste de resiliência — RabbitMQ

Demonstra que o outbox garante entrega mesmo com o broker indisponível.

```bash
# 1. Parar o RabbitMQ
docker stop <rabbitmq-container-id>
# No Aspire, use o botão Stop no recurso RabbitMQ.

# 2. Criar uma compra (cai no outbox)
curl -s -X POST {bookstoreUrl}/api/v1/purchases \
  -H "Idempotency-Key: resilience-001" \
  -H "Content-Type: application/json" \
  -d '{"bookId":"11111111-0000-0000-0000-000000000001","quantity":1,"customerId":"c1","paymentType":"CreditCard","paymentFingerprint":"fp1","paymentLast4":"0000"}'

# 3. Verificar: compra em PendingFraudCheck, outbox_pending > 0 no Grafana

# 4. Restaurar o RabbitMQ
docker start <rabbitmq-container-id>

# 5. Aguardar ~10 s; a compra avança para Confirmed automaticamente
```

---

## 7. Dashboards Grafana

Acesse http://localhost:3000 (senha: `bookstore`).

| Dashboard | UID | Conteúdo |
|---|---|---|
| BookStore — Fraude | `bookstore-fraud-v1` | Visão geral legada |
| Antifraude — Negócio | `antifraude-negocio-v1` | Decisões, decisores, regras, compras, aprovação % |
| Antifraude — Operação | `antifraude-operacao-v1` | Latência p95, outbox, replay, RabbitMQ queue depth |

### Links diretos (requer Grafana na porta 3000)

- Negócio: http://localhost:3000/d/antifraude-negocio-v1
- Operação: http://localhost:3000/d/antifraude-operacao-v1

### O que observar durante a demo

1. **`fraud_decisions_total`** sobe por `decider` (ENGINE na avaliação, REVIEWER na revisão manual).
2. **`fraud_reviews_pending`** aumenta quando uma compra vai para Review e cai após a revisão.
3. **`messaging_outbox_pending`** sobe durante a falha do RabbitMQ e retorna a 0 após recuperação.
4. **`rabbitmq_queue_messages_ready`** mostra o backlog de mensagens aguardando o consumer.
5. **`fraud_decision_duration_seconds`** p95 ≈ delay configurado (30 s em demo) + processamento.
