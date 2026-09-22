---
id: BERT-TSK-0062
title: "TSK-0062 — Composição: DI segmentada, JSON, erros, versão e Swagger"
type: task
versão: "1.0.0"
status: pendente
executor: claude-code
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-22
governed_by:
- '[[00-BERT-ST-0040-fundacao-da-solucao]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/01-BERT-ST-0040-fundacao-da-solucao/03-BERT-TSK-0062-composicao-das-aplicacoes.md
description: "DependencyInjection segmentado, extensions por tecnologia, enums SNAKE_UPPER, Problem Details, grupo /api/v1 e Swagger UI."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 01-BERT-ST-0040-fundacao-da-solucao, 03-BERT-TSK-0062-composicao-das-aplicacoes]
---

## Descrição

- `DependencyInjection.cs` de Application e Infrastructure **segmentados** por responsabilidade
  (`AddPersistence()`, `AddMessaging()`, `AddTelemetry()`…), cada método com `<summary>` **em inglês**.
- Extensions por tecnologia em `src/Infrastructure/Extensions/` (`PostgresExtensions`,
  `MassTransitExtensions`, `TelemetryExtensions`). Aqui só os arquivos e assinaturas; o conteúdo
  chega nas tasks de persistência, mensageria e telemetria.
- JSON: `JsonStringEnumConverter` com `JsonNamingPolicy.SnakeCaseUpper` **global** — a API devolve
  `APPROVED`, `PENDING_FRAUD_CHECK` (D-64). No código os enums seguem PascalCase.
- Erros em Problem Details (RFC 9457): `AddProblemDetails()` + tratamento de exceção.
- `MapGroup("/api/v1")` — os endpoints nunca repetem a versão (ADR-0006).
- OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) + **Swagger UI** (`Swashbuckle.AspNetCore.SwaggerUI`)
  apontando para o documento nativo (D-62).
- WebApi e Worker compõem **só o que usam**.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

**Padrões .NET (§8 do prompt) — esta task cria a base:**

- Pacotes: `Cortex.Mediator` (3.x estável, MIT — nunca MediatR) e `FluentValidation` (+ extensão de DI).
- `Application/Commons/ErrorCode.cs` e `OperationResult.cs` (base não genérica + genérica, `init`).
- `Application/Behaviors/`: guarda de exceção e `ValidationBehavior` (devolve `Fail(Validation)`, não lança),
  registrados no Cortex para comandos e consultas, em `Application/DependencyInjection.cs`.
- WebApi: extensão `ToHttpResult()` (tabela da §8.2, incluindo `Unprocessable → 422`) e
  `GlobalExceptionHandler : IExceptionHandler` (§8.7).

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/DependencyInjection.cs
src/Infrastructure/DependencyInjection.cs
src/Infrastructure/Extensions/**
apps/WebApi/Program.cs
apps/Worker/Program.cs
apps/WebApi/appsettings*.json
apps/Worker/appsettings*.json
tests/Tests.WebApi/**
src/Application/Commons/**
src/Application/Behaviors/**
apps/WebApi/Extensions/**
apps/WebApi/ErrorHandling/**
tests/Tests.Application/Behaviors/**
```

## Critério de Aceite

- [ ] Todo método público de extension/DI tem `<summary>` em inglês
- [ ] Um enum serializado pela API sai em maiúsculas com underscore (teste)
- [ ] Rotas da API ficam sob `/api/v1` via `MapGroup`
- [ ] Swagger UI abre em ambiente de desenvolvimento
- [ ] Erro não tratado responde `application/problem+json`
- [ ] `ValidationBehavior` devolve `Fail(Validation)` sem lançar (teste)
- [ ] `ToHttpResult()` mapeia cada `ErrorCode` para o status da §8.2 (teste)
- [ ] Program não chama `AddCortexMediator` diretamente
- [ ] Nenhuma referência a MediatR

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.WebApi --filter "FullyQualifiedName~Serialization|FullyQualifiedName~HttpResult"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados; prova `APPROVED`/`PENDING_FRAUD_CHECK` na serialização e o mapeamento `ErrorCode` → HTTP

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~Behavior"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados; `ValidationBehavior` devolve falha sem lançar


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
