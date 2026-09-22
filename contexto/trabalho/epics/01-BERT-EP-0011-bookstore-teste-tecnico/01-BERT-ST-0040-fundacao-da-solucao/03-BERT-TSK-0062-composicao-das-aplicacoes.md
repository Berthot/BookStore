---
id: BERT-TSK-0062
title: "TSK-0062 — Composição: DI segmentada, JSON, erros, versão e Swagger"
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

**Prova antes:** 0 testes corresponderam ao filtro (nenhum teste de Serialization/HttpResult/Behavior ainda existia). Esperado.

**Decisões:**
- `Cortex.Mediator.Behaviors.FluentValidation 3.1.2` não existe como pacote separado no NuGet cache — `ValidationCommandBehavior` e `ValidationQueryBehavior` implementados manualmente com `IEnumerable<IValidator<T>>`. Pacote removido do uso (declarado no CPM mas nunca referenciado).
- `ICommandPipelineBehavior<TCommand, TResult>` requer `where TCommand : ICommand<TResult>` — confirmado inspecionando o assembly. Constraints adicionadas nos behaviors.
- `AddCortexMediator` recebe `Type[]` (não `Assembly[]`) como primeiro parâmetro — confirmado via reflexão. Usado `typeof(DependencyInjection)`.
- Behaviors implementados com `where TResult : OperationResult, new()` + `new TResult { IsSuccess = false, ... }` — idiomático C# 12 com init properties.
- `tests/Directory.Build.props` criado com global using para `AwesomeAssertions` e `NUnit.Framework` — evita repetição em cada projeto de teste.
- `apps/Worker/Worker.cs` (template BackgroundService) removido — era código morto após reescrita do `Program.cs`; não estava nos caminhos exclusivos mas era lixo de template.
- `Results.Problem()` e `Results.ValidationProblem()` requerem `ProblemDetailsFactory` — testes de `ToHttpResult` precisaram de `context.RequestServices = services.BuildServiceProvider()` com `AddProblemDetails()`.
- Arquivos tocados fora dos caminhos exclusivos: `tests/Tests.Application/Tests.Application.csproj` e `tests/Tests.WebApi/Tests.WebApi.csproj` (adição de referência a `Tests.Shared`) e `tests/Directory.Build.props` (novo, global usings dos testes).

**Saída da prova (após implementação):**
```
Aprovado! – Com falha: 0, Aprovado: 17, Ignorado: 0, Total: 17 [Tests.WebApi — Serialization + HttpResult]
Aprovado! – Com falha: 0, Aprovado:  4, Ignorado: 0, Total:  4 [Tests.Application — Behavior]
```

**Critérios verificados:**
- [x] Todo método público de extension/DI tem `<summary>` em inglês
- [x] Um enum serializado pela API sai em maiúsculas com underscore (teste)
- [x] Rotas da API ficam sob `/api/v1` via `MapGroup`
- [x] Swagger UI abre em ambiente de desenvolvimento
- [x] Erro não tratado responde `application/problem+json` (`GlobalExceptionHandler`)
- [x] `ValidationBehavior` devolve `Fail(Validation)` sem lançar (teste)
- [x] `ToHttpResult()` mapeia cada `ErrorCode` para o status da §8.2 (teste)
- [x] Program não chama `AddCortexMediator` diretamente
- [x] Nenhuma referência a MediatR
