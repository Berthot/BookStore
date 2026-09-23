# ADR-0005 — Observabilidade e correlação

- **Status:** aceito
- **Data:** 2026-09-22

## Contexto

Uma compra atravessa API, outbox, RabbitMQ, Worker, antifraude e volta ao marketplace — em processos
e momentos diferentes. Sem correlação, investigar "o que aconteceu com esta compra?" significa
procurar em logs soltos. O desafio pede **métricas, logs estruturados e tracing**, e a auditoria
precisa ligar uma decisão ao fluxo que a produziu.

## Opções consideradas

| Opção | A favor | Contra |
| :--- | :--- | :--- |
| **SDK de um fornecedor** (Application Insights, Datadog…) | painel pronto | acopla o código ao fornecedor; trocar exige reescrever instrumentação |
| **Só logs estruturados** | simples | não mostra a sequência entre processos nem o tempo de cada etapa |
| **OpenTelemetry (traces, métricas e logs) exportando por OTLP** | padrão aberto; instrumentação nativa em ASP.NET Core, EF Core/Npgsql e MassTransit; o destino é configuração | mais uma dependência a configurar |

## Decisão

**OpenTelemetry nos dois processos (API e Worker), exportando por OTLP.** Na demonstração, o destino é
o **Aspire Dashboard** (container no docker-compose, ou embutido no Aspire AppHost em desenvolvimento).
Métricas de negócio também são exportadas para Prometheus + Grafana quando rodando via Aspire (ADR-0009).

Toda a configuração fica em `TelemetryExtensions`, reutilizada pela API e pelo Worker.

### Tracing

- **W3C Trace Context** (`traceparent`) propagado automaticamente em HTTP e nas mensagens do MassTransit:
  uma compra aparece como **um único trace**, da requisição à atualização final da compra.
- Spans próprios nas etapas de negócio: avaliação das regras, decisão da política, gravação da decisão.

### Correlação

Três identificadores, com papéis diferentes:

| Identificador | Papel | Origem |
| :--- | :--- | :--- |
| `Idempotency-Key` | deduplicar a mesma requisição | cliente |
| `TransactionId` / `PurchaseId` | identidade do recurso | sistema |
| `CorrelationId` | ligar tudo o que pertence ao mesmo fluxo de negócio | cliente, via `X-Correlation-Id`; gerado pela API se ausente |

O `CorrelationId` é devolvido no header da resposta (e só nele), viaja nas mensagens, entra em todo log (escopo de
log) e fica **persistido na transação** — permitindo ir do registro no banco aos logs, e vice-versa.
O `traceparent` serve às ferramentas; o `CorrelationId` serve às pessoas e à auditoria.

**Por que não usar só o `traceId`?** O trace descreve **uma execução**; o `CorrelationId` descreve **um
fluxo de negócio**, que pode atravessar várias execuções ao longo de dias:

| Situação | `traceId` | `CorrelationId` |
| :--- | :--- | :--- |
| compra processada normalmente | o mesmo de ponta a ponta | o mesmo de ponta a ponta |
| reconciliação republica a compra, minutos ou horas depois | novo trace (o job inicia a execução) | o mesmo — gravado na compra e reenviado |
| revisor decide a transação dias depois | novo trace (outra requisição HTTP) | o mesmo — gravado na transação |
| trace descartado por amostragem em produção | perdido | preservado no banco e nos logs |

Por isso os dois coexistem sem redundância. Na API, a correlação aparece **só** no header
`X-Correlation-Id` (entrada e saída), nunca no corpo JSON. Internamente, é uma única propriedade,
`CorrelationId`, gravada no banco e carregada nas mensagens.

### Logs estruturados

- `ILogger` com *message templates* (campos nomeados, nunca texto concatenado).
- Campos presentes em todo log de negócio: `CorrelationId`, `TransactionId` ou `PurchaseId`, `TraceId`.
- **Nunca** registrar dado de cartão — apenas o `Fingerprint`.

### Métricas

| Métrica | Tipo | Para quê |
| :--- | :--- | :--- |
| `fraud.transactions.received` | contador | volume de entrada |
| `fraud.decisions` (por `outcome`) | contador | proporção aprovado / rejeitado / revisão |
| `fraud.rule.hits` (por `rule_code`) | contador | quais regras mais disparam |
| `fraud.decision.duration` | histograma | tempo entre recebimento e decisão |
| `bookstore.purchases` (por `status`) | contador | vendas efetivas vs canceladas |
| `idempotency.replays` | contador | frequência de reenvios dos clientes |

Somam-se as métricas técnicas das instrumentações (HTTP, runtime, banco, mensageria).

## Consequências

**Positivas**

- Um fluxo inteiro é investigável por um único identificador, no painel ou no banco.
- O código depende só de um padrão aberto; o destino é configuração.
- As métricas de negócio respondem perguntas de operação ("a regra X está disparando demais?").

**Negativas e mitigação**

- **O Aspire Dashboard guarda os dados só em memória.** Serve para demonstração; em produção, o OTLP
  apontaria para um backend persistente.
- **Painel sem autenticação na demonstração** (`ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`).
  Aceitável só em ambiente local.
- **Volume de telemetria em produção.** Mitigação futura: amostragem de traces.
