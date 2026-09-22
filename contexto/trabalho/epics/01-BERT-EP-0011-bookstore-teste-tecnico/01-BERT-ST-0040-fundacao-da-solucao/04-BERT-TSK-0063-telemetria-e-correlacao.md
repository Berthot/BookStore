---
id: BERT-TSK-0063
title: "TSK-0063 — Telemetria (OpenTelemetry) e X-Correlation-Id"
type: task
versão: "1.0.0"
status: concluido
executor: claude-code
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-22
governed_by:
- '[[00-BERT-ST-0040-fundacao-da-solucao]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/01-BERT-ST-0040-fundacao-da-solucao/04-BERT-TSK-0063-telemetria-e-correlacao.md
description: "OpenTelemetry via OTLP em TelemetryExtensions e middleware de X-Correlation-Id (gera, devolve, põe no escopo de log)."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 01-BERT-ST-0040-fundacao-da-solucao, 04-BERT-TSK-0063-telemetria-e-correlacao]
---

## Descrição

- `TelemetryExtensions.AddTelemetry(...)`: traces, métricas e logs OpenTelemetry exportando por OTLP
  (`OTEL_EXPORTER_OTLP_ENDPOINT`). Instrumentações de ASP.NET Core, HttpClient, EF Core/Npgsql e
  MassTransit (a de MassTransit entra quando o bus existir). Reutilizada por WebApi e Worker.
- Middleware de correlação: lê `X-Correlation-Id`; se ausente, gera. **Sempre** devolve no header da
  resposta e abre um escopo de log com `CorrelationId`. Na API a correlação vive **só no header** —
  nunca no corpo JSON (D-60).
- Logs com *message templates*, nunca concatenação. Nunca registrar dado de cartão.

Por quê: o `CorrelationId` descreve o fluxo de negócio e sobrevive a reconciliação e revisão; o
`traceId` descreve uma execução (ADR-0005).

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Infrastructure/Extensions/TelemetryExtensions.cs
apps/WebApi/Middleware/**
apps/WebApi/Program.cs
apps/Worker/Program.cs
tests/Tests.WebApi/Middleware/**
```

## Critério de Aceite

- [ ] Requisição sem `X-Correlation-Id` recebe um gerado no header da resposta
- [ ] Requisição com `X-Correlation-Id` recebe o mesmo valor de volta
- [ ] O valor entra no escopo de log
- [ ] Nenhum corpo de resposta contém `correlationId`
- [ ] Endpoint OTLP vem de configuração, não de código

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.WebApi --filter "FullyQualifiedName~Correlation"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 2 testes (com e sem header)


## Notas de execução

**Prova antes:** 0 testes corresponderam ao filtro. Esperado.

**Decisões:**
- `TelemetryExtensions.AddTelemetry(IConfiguration configuration)` preenche OTLP via `OTEL_EXPORTER_OTLP_ENDPOINT` (variável de ambiente nativa do OTEL SDK — zero hardcode).
- `AddNpgsql()` (Npgsql.OpenTelemetry) omitido por ora — conflitava com o `AddNpgsql<TContext>()` do EF Core que ainda não existe. Será adicionado em TSK-0069 após os DbContexts serem configurados.
- `CorrelationIdMiddleware.InvokeAsync` seta o header diretamente em vez de via `OnStarting` — `OnStarting` não dispara sem escrita real na resposta, o que impedia os testes unitários. Em produção a diferença é irrelevante (middleware roda antes de qualquer escrita); em testes a abordagem direta funciona corretamente.
- `Infrastructure/DependencyInjection.cs` atualizado para passar `configuration` para `AddTelemetry` — fora dos caminhos exclusivos mas necessário; registrado aqui.

**Saída da prova (após implementação):**
```
Aprovado! – Com falha: 0, Aprovado: 3, Ignorado: 0, Total: 3 [Tests.WebApi — Correlation]
```

**Critérios verificados:**
- [x] Requisição sem `X-Correlation-Id` recebe um gerado no header da resposta
- [x] Requisição com `X-Correlation-Id` recebe o mesmo valor de volta
- [x] O valor entra no escopo de log (`logger.BeginScope`)
- [x] Nenhum corpo de resposta contém `correlationId` (header apenas)
- [x] Endpoint OTLP vem de `OTEL_EXPORTER_OTLP_ENDPOINT` (env var), não de código
