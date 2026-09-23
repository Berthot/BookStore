# ADR-0009 — Extras de apresentação

- **Status:** aceito
- **Data:** 2026-09-23

## Contexto

Após a entrega do EP-0011 (feature completa), um conjunto de melhorias foi aplicado para tornar o
projeto mais robusto e visualmente demonstrável em apresentação. Este ADR consolida todas as adições
fora do escopo funcional original, servindo de guia rápido para a apresentação.

---

## 1. Migração do AppHost para Aspire.AppHost.Sdk

### Problema

O projeto `apps/AppHost` usava a workload `aspire` (`dotnet workload install aspire`), que está
obsoleta no .NET 10. O DCP e o Dashboard não eram encontrados, resultando em:

```
Property CliPath: The path to the DCP executable used for Aspire orchestration is required.
Property DashboardPath: The path to the Aspire Dashboard binaries is missing.
```

### Decisão

Criar `apps/Aspire/Aspire.csproj` usando o **NuGet MSBuild SDK**:

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.5.4">
  <PropertyGroup>
    <AspireUseCliBundle>true</AspireUseCliBundle>
  </PropertyGroup>
</Project>
```

Instalar a ferramenta global `aspire.cli` com a versão que casa exatamente com o SDK:

```bash
dotnet tool install --global aspire.cli --version 13.5.4
```

> A versão do `aspire.cli` deve ser idêntica à do `Aspire.AppHost.Sdk` no csproj.
> Para conferir a versão disponível: `dotnet tool search aspire.cli`.

### Consequências

- Um único comando sobe toda a stack em desenvolvimento: `aspire run --project apps/Aspire`
- Sem dependência de workload instalada na máquina; apenas `aspire.cli` como ferramenta global
- `apps/AppHost` removido da solução

---

## 2. Stack de observabilidade para apresentação

### Justificativa da divisão de responsabilidades

O **Aspire Dashboard** é a ferramenta de desenvolvimento e debug: mostra traces distribuídos (flame
graph, spans, atributos), logs estruturados com busca e correlação, saúde dos recursos e métricas
técnicas do runtime. É efêmero — os dados vivem em memória apenas enquanto a sessão está ativa.

O **Prometheus + Grafana** responde à pergunta de negócio: quantas decisões de fraude foram tomadas,
quantas foram bloqueadas, quais regras dispararam mais, qual é a latência p95 do motor. O dashboard
pré-provisionado tem refresh de 5 s e é o que se mostra na tela durante a demo enquanto a coleção
Postman roda — os números sobem em tempo real.

Os dois coexistem sem conflito: métricas vão para os dois destinos via dois exporters OTLP independentes;
traces e logs vão apenas para o Aspire Dashboard.

### Containers adicionados ao Aspire

| Container | Imagem | Porta host | Para quê |
| :--- | :--- | :--- | :--- |
| `prometheus` | `prom/prometheus:v2.55.0` | 9090 | Ingestão de métricas via OTLP push |
| `grafana` | `grafana/grafana:11.4.0` | 3000 | Dashboard pré-provisionado de fraude |

### Roteamento dos sinais

| Sinal | Destino |
| :--- | :--- |
| Traces | Aspire Dashboard (exporter padrão) |
| Métricas | Aspire Dashboard + **Prometheus** (segundo exporter OTLP) |
| Logs | Aspire Dashboard (inalterado) |

### Segundo exporter de métricas em `TelemetryExtensions.cs`

```csharp
var prometheusBase = configuration["PROMETHEUS_OTLP_ENDPOINT"];
if (!string.IsNullOrEmpty(prometheusBase))
    metrics.AddOtlpExporter(o =>
    {
        // SDK appends /v1/metrics automatically (http/protobuf)
        o.Endpoint = new Uri(prometheusBase.TrimEnd('/') + "/api/v1/otlp");
        o.Protocol = OtlpExportProtocol.HttpProtobuf;
    });
```

`PROMETHEUS_OTLP_ENDPOINT` é injetado pelo Aspire via `EndpointReference` — o AppHost resolve o
URL em tempo de execução a partir do container `prometheus` (porta 9090).

### Prometheus — OTLP push receiver

Prometheus iniciado com `--web.enable-otlp-receiver`; apps enviam métricas diretamente via OTLP
HTTP/protobuf. Sem package extra no código da aplicação.

### Grafana — provisionamento automático

Arquivos em `apps/Aspire/grafana/`:

```
provisioning/
  datasources/prometheus.yaml   → datasource "Prometheus" apontando para host.docker.internal:9090
  dashboards/provider.yaml      → aponta para /var/lib/grafana/dashboards
dashboards/
  bookstore.json                → dashboard "BookStore — Fraude" (uid: bookstore-fraud-v1)
```

Dashboard pré-provisionado com 7 painéis (refresh 5 s, janela 15 min):

| Painel | Tipo | Métrica |
| :--- | :--- | :--- |
| Decisões (5 min) | stat | `sum(increase(fraud_decisions_total[5m]))` |
| Bloqueadas (5 min) | stat | `fraud_decisions_total{outcome="Rejected"}` |
| Em Revisão (5 min) | stat | `fraud_decisions_total{outcome="Review"}` |
| Decisões por Resultado | pie | `sum by (outcome)(increase(fraud_decisions_total[5m]))` |
| Regras Mais Ativadas | bar gauge | `topk(8, sum by (rule_code)(increase(fraud_rule_hits_total[5m])))` |
| Latência p95 (s) | timeseries | `histogram_quantile(0.95, ...)` sobre `fraud_decision_duration_seconds` |
| Taxa de Decisões/min | timeseries | `rate(fraud_decisions_total[1m]) * 60` |

**Acesso Grafana:** `http://localhost:3000` — anônimo com role Viewer (sem login em demonstração).
Credenciais para edição de painéis: `admin` / `bookstore`.

### Arquivos de configuração e CopyToOutputDirectory

`prometheus.yml` e `grafana/**` são bind-montados nos containers. O Aspire resolve caminhos relativos
a partir do diretório de output (`bin/Debug/net10.0/`), não do diretório-fonte, então os arquivos
precisam ser copiados via MSBuild:

```xml
<Content Include="prometheus.yml">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
<Content Include="grafana\**\*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

### Consequências

- As métricas de negócio ficam em um dashboard dedicado, com atualização em tempo real durante a demo
- O Aspire Dashboard continua sendo a ferramenta de debug para traces e logs
- Adicionar mais containers aumenta o tempo de primeira inicialização (pull de imagens)

---

## 3. Melhorias de OTel nos serviços

### 3.1 Spans de avaliação como irmãos (não filhos)

**Problema:** `evalSpan` e `decideSpan` estavam aninhados (`using var` no mesmo escopo), fazendo o
`decideSpan` aparecer como filho do `evalSpan` no trace.

**Solução:** Blocos `{ }` explícitos garantem que `evalSpan` seja descartado antes de `decideSpan`
abrir, tornando-os irmãos:

```csharp
{ using var evalSpan = ...; /* avaliação das regras */ }
{ using var decideSpan = ...; /* decisão e publicação */ }
```

### 3.2 Nome de unidade da duração

`fraud.decision.duration` estava com unidade `"ms"` (milissegundos). Corrigido para `"s"` (segundos),
seguindo as convenções semânticas do OTel.

### 3.3 Nome da métrica de replay

`"idempotency.replays"` → `"bookstore.idempotency.replays"` (namespace consistente com as demais métricas).

### 3.4 Filter de escopo de log no MassTransit

Adicionado `LoggingScopeConsumeFilter<T>` em `src/Infrastructure/Messaging/` que injeta
`CorrelationId`, `MessageType` e `MessageId` no escopo de log de todos os consumers, eliminando a
necessidade de logar esses campos manualmente em cada consumer.

Registrado via:
```csharp
cfg.UseConsumeFilter(typeof(LoggingScopeConsumeFilter<>), context);
```

### 3.5 Redução de ruído nos logs

Filtros de log injetados pelo Aspire (ProjectExtensions.cs):

| Categoria | Nível anterior | Nível atual |
| :--- | :--- | :--- |
| `Microsoft.EntityFrameworkCore.Database.Command` | Warning | — (removido) |
| `Microsoft.EntityFrameworkCore` | Information | **Warning** |
| `Npgsql` | Information | **Warning** |

---

## 4. Melhorias na API

### 4.1 Endpoint de health check

Adicionado `GET /health` (via `builder.Services.AddHealthChecks()` + `app.MapHealthChecks("/health")`).
Usado pelo Aspire para aguardar a WebApi estar pronta antes de subir o Worker (`.WithHttpHealthCheck`).

### 4.2 Serialização de enums como SNAKE_UPPER

Todos os enums serializam como `SNAKE_UPPER` via `JsonNamingPolicy.SnakeCaseUpper`:

| Enum | Valores |
| :--- | :--- |
| `PurchaseStatus` | `PENDING_FRAUD_CHECK`, `CONFIRMED`, `CANCELLED`, `UNDER_REVIEW` |
| `Outcome` | `APPROVED`, `REJECTED`, `REVIEW` |
| `TransactionStatus` | `RECEIVED`, `PROCESSING`, `DECIDED` |

### 4.3 FraudDetails enriquecido no GET /purchases/:id

A resposta de `GET /purchases/:id` passou a incluir, dentro de `fraudDetails`:

```json
{
  "transactionId": "...",
  "outcome": "REJECTED",
  "score": 85,
  "triggeredRules": [
    { "code": "CARD_VELOCITY", "reason": "5 transações com o mesmo cartão em 30 dias" }
  ]
}
```

### 4.4 Correção do AsSplitQuery

O repositório `TransactionRepository` carregava `Transaction → Assessments → Evaluations` em uma
única query SQL (produto cartesiano entre duas coleções), disparando o warning EF Core 20504.
Corrigido adicionando `.AsSplitQuery()` nas queries `GetByIdAsync` e `GetByCorrelationIdAsync`.

---

## 5. Coleções Postman

### Arquivos em `docs/postman/`

| Arquivo | Descrição |
| :--- | :--- |
| `BookStore.postman_collection.json` | Coleção com cenários S1–S11 |
| `local.postman_environment.json` | Docker Compose — `baseUrl: http://localhost:8080` |
| `local-aspire.postman_environment.json` | Aspire HTTP — `baseUrl: http://localhost:5085` |
| `local-https.postman_environment.json` | Aspire HTTPS — `baseUrl: https://localhost:7270` |

Portas:
- **Docker Compose:** `http://localhost:8080` → use `local.postman_environment.json`
- **Aspire HTTP:** `http://localhost:5085` → use `local-aspire.postman_environment.json`
- **Aspire HTTPS:** `https://localhost:7270` → use `local-https.postman_environment.json`

Para HTTPS: desabilitar "SSL certificate verification" em Postman → Settings → General.

> **S9 é manual** — não há requests Postman para S9. O cenário exige parar e reiniciar o Worker
> via linha de comando enquanto mantém uma compra em trânsito.

### Melhorias na coleção

- `GET /health` adicionado na pasta Catálogo (smoke test antes de rodar cenários)
- **S3.2 e S10.5:** asserções dos novos campos `fraudDetails.score`, `fraudDetails.outcome` e
  `fraudDetails.triggeredRules` quando o status chega a `UNDER_REVIEW`
- **S4.3:** loop de retry para aguardar `CONFIRMED` (o Worker processa o evento de forma assíncrona)
- **S10.5:** loop de retry adicionado (estava ausente)

---

## 6. Automação Newman

### Problema

Executar os 11 cenários manualmente no Postman durante a apresentação é propenso a erros e lento.

### Decisão

Criar `docs/postman/run-demo.ps1` — script PowerShell que executa a coleção inteira (ou um cenário
específico) via Newman e abre um relatório HTML automaticamente ao final.

**Pré-requisitos (uma vez):**

```powershell
npm install -g newman newman-reporter-htmlextra
```

**Uso típico:**

```powershell
# Todos os cenários (Aspire HTTP :5085)
.\docs\postman\run-demo.ps1

# Cenário isolado
.\docs\postman\run-demo.ps1 -Folder 'S3'

# Docker Compose :8080
.\docs\postman\run-demo.ps1 -Environment docker

# HTTPS + delay maior para Worker lento
.\docs\postman\run-demo.ps1 -Environment https -Delay 600
```

O script:
1. Verifica se Newman está instalado
2. Faz health check em `/health` antes de rodar
3. Executa Newman com reporters `cli` (terminal) + `htmlextra` (HTML)
4. Salva o relatório em `docs/postman/newman/report-<timestamp>.html`
5. Abre o relatório no browser automaticamente

O diretório `docs/postman/newman/` está no `.gitignore`.

### Consequências

- Rodar todos os cenários vira um único comando — reproduzível em CI se necessário
- O relatório HTML do `newman-reporter-htmlextra` mostra assertions individuais, timings e logs de console dos scripts Postman

---

## Referências rápidas para a apresentação

| O que mostrar | Onde abrir |
| :--- | :--- |
| Serviços rodando e saúde | Aspire Dashboard → aba Resources |
| Trace de uma compra (WebApi → Worker) | Aspire Dashboard → aba Traces |
| Métricas de fraude em tempo real | Grafana → `http://localhost:3000` → dashboard "BookStore — Fraude" |
| Logs estruturados com CorrelationId | Aspire Dashboard → aba Structured |
| Métricas técnicas (runtime, HTTP) | Aspire Dashboard → aba Metrics |
| Documentação da API | `http://localhost:5085/swagger` |
| RabbitMQ Management | `http://localhost:15672` (guest/guest) |
