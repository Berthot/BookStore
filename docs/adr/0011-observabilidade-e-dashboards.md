# ADR-0011 — Observabilidade e Dashboards Grafana

**Status:** Aceito  
**Data:** 2026-09-24  
**Contexto:** Split em três processos (BookStore.Api, Fraud.Api, Fraud.Worker) — ADR-0010

---

## Contexto

Com a separação em três processos independentes, é necessário observar o comportamento do sistema
tanto do ponto de vista de negócio (decisões de fraude, aprovações, revisões) quanto operacional
(latência, saúde das filas, outbox, idempotência). A coleta de métricas deve ser uniforme entre
os processos e exibida em dashboards Grafana prontos para demo.

---

## Decisão

### Instrumentação OpenTelemetry

Todos os processos publicam métricas via **OpenTelemetry OTLP push** para o Prometheus rodando no
Aspire com `--enable-feature=otlp-write-receiver`.

**Meter `FraudAnalysis` (Fraud.Api + Fraud.Worker):**

| Métrica | Tipo | Tags | Descrição |
|---|---|---|---|
| `fraud.decisions` | Counter | `outcome`, `decider` | Decisões emitidas por ENGINE, SYSTEM ou REVIEWER |
| `fraud.rule.hits` | Counter | `rule_code` | Ativações de regras de fraude |
| `fraud.decision.duration` | Histogram | `outcome` | Tempo (s) entre criação e decisão |
| `fraud.idempotency.requests` | Counter | `result` (new\|replay) | Requisições idempotentes |
| `fraud.reviews.pending` | Gauge | — | Transações aguardando revisão humana |
| `messaging.outbox.pending` | Gauge | `context=fraud` | Mensagens não entregues no outbox |

**Meter `BookStore` (BookStore.Api):**

| Métrica | Tipo | Tags | Descrição |
|---|---|---|---|
| `bookstore.purchases` | Counter | `status` | Transições de estado de compras |
| `bookstore.idempotency.requests` | Counter | `result` (new\|replay) | Requisições idempotentes |
| `bookstore.reconciliation.republished` | Counter | — | Compras re-publicadas pela reconciliação |
| `messaging.outbox.pending` | Gauge | `context=bookstore` | Mensagens não entregues no outbox |

### Gauges com BackgroundService

`fraud.reviews.pending` e ambos os `messaging.outbox.pending` são valores pontuais (não contadores),
atualizados a cada **30 segundos** por `FraudMetricsJob` e `BookStoreMetricsJob`. Cada job cria um
scope de DI, consulta o banco e chama `FraudTelemetry.Set*` / `BookStoreTelemetry.Set*` com
`Volatile.Write` para evitar race conditions.

### RabbitMQ Prometheus Plugin

O plugin `rabbitmq_prometheus` (incluído na imagem `4-management-alpine`) expõe métricas nativas
em `/metrics` na porta **15692**. O Aspire expõe essa porta com `WithHttpEndpoint(name:"prometheus")`
e o `prometheus.yml` inclui um `scrape_configs` apontando para `host.docker.internal:15692`.
Isso permite monitorar `rabbitmq_queue_messages_ready` sem agente externo.

### Dashboards Grafana

Dois dashboards provisionados automaticamente via bind-mount em `/var/lib/grafana/dashboards`:

**`antifraude-negocio.json`** — visão de produto:
- Stats: total de decisões, aprovações, rejeições, reviews pendentes
- Pizza: distribuição por resultado e por decisor (ENGINE / SYSTEM / REVIEWER)
- Bargauge: top-8 regras mais ativadas
- Timeseries: compras por status, taxa de aprovação %

**`antifraude-operacao.json`** — visão de plataforma:
- Stats: outbox pendentes (fraud + bookstore), reconciliação re-publicadas, replay rate
- Timeseries: latência p50/p95, throughput por decisor, outbox trend, RabbitMQ queue depth
- Timeseries: idempotency new vs replay por contexto

---

## Consequências

**Positivas:**
- Visibilidade imediata após `dotnet run --project apps/Aspire` — Grafana abre na porta 3000 com dashboards pré-carregados.
- O tag `decider` em `fraud.decisions` permite distinguir ENGINE, SYSTEM (fail-safe) e REVIEWER sem joins no banco.
- O tag `context` em `messaging.outbox.pending` consolida os dois outboxes em um único painel.
- RabbitMQ scrape nativo elimina dependência de exporter externo.

**Negativos / Trade-offs:**
- Gauges com janela de 30 s introduzem latência de atualização — aceitável para demo.
- OTLP push requer que o Prometheus tenha o flag `--enable-feature=otlp-write-receiver` (disponível desde v2.39 com flag experimental; estável em v3.x). A imagem fixa `v2.55.0` suporta.
- Os dashboards usam UID estático (`antifraude-negocio-v1`, `antifraude-operacao-v1`) — conflito se importados manualmente em instância com UID existente.
