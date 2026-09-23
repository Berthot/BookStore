---
id: BERT-TSK-0073
title: "TSK-0073 — Filtro HTTP de idempotência (400, replay, 422, 409)"
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
- '[[00-BERT-ST-0043-antifraude-api-e-worker]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/04-BERT-ST-0043-antifraude-api-e-worker/01-BERT-TSK-0073-filtro-de-idempotencia.md
description: "Endpoint filter compartilhado pelos POSTs: header obrigatório, hash do corpo, replay, 422 e 409 com lock_timeout curto."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 04-BERT-ST-0043-antifraude-api-e-worker, 01-BERT-TSK-0073-filtro-de-idempotencia]
---

## Descrição

Comportamento do draft IETF `draft-ietf-httpapi-idempotency-key-header-07` (ADR-0004):

| Situação | Resposta |
| :--- | :--- |
| header ausente | `400` |
| corpo inválido | `400` **sem registrar a chave** |
| chave nova | segue para o caso de uso |
| repetida, mesmo hash | replay da resposta original |
| repetida, hash diferente | `422` |
| original em execução | `409` |

Concorrência: a segunda requisição espera o índice único com `lock_timeout` curto (`SET LOCAL`); se a
primeira confirmou → replay; se esgotou → `409`. Se a execução falhar, o rollback desfaz a chave e o
cliente pode reenviar (diferença consciente em relação ao Stripe).

Um componente só, reutilizado pelos dois `POST`. Declarar `Idempotency-Key` **obrigatório no OpenAPI**
— sem isso o Swagger não mostra o campo.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
apps/WebApi/Filters/Idempotency/**
apps/WebApi/OpenApi/**
tests/Tests.WebApi/Filters/**
```

## Critério de Aceite

- [x] Os seis casos da tabela têm teste (store substituído por NSubstitute)
- [x] O filtro não chama `SaveChanges`
- [x] Documento OpenAPI declara `Idempotency-Key` como header obrigatório nos POSTs

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.WebApi --filter "FullyQualifiedName~Idempotency"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 6 testes


## Notas de execução

- `IdempotencyFilter<TStore>` genérico em `apps/WebApi/Filters/Idempotency/` — recebe `TStore : IIdempotencyStore`; os dois POSTs (Fraud e BookStore) usam o mesmo filtro com o store tipado certo.
- O filtro habilita `EnableBuffering()` para que o body possa ser lido pelo model binding depois.
- Não chama `SaveChanges` diretamente — apenas chama `store.Add(entry)` e coloca a entrada em `HttpContext.Items["IdempotencyEntry"]`; o `CommitAsync` do use case confirma tudo.
- `IdempotencyKeyExtensions.RequireIdempotencyKey()` em `apps/WebApi/OpenApi/` adiciona o header obrigatório ao OpenAPI. `WithOpenApi(Func<...>)` é deprecated no .NET 10 (ASPDEPR002) — suprimido via pragma até que a extensão seja remodelada como `IOpenApiDocumentTransformer`.
- `Microsoft.OpenApi` no .NET 10 moveu os tipos de `Microsoft.OpenApi.Models` para `Microsoft.OpenApi` diretamente; `Schema.Type` é agora `JsonSchemaType?` (enum), não `string`.

**Prova:**
```
dotnet test tests/Tests.WebApi --filter "FullyQualifiedName~Idempotency"
Aprovado!  – Com falha: 0, Aprovado: 6, Ignorado: 0, Total: 6
```
