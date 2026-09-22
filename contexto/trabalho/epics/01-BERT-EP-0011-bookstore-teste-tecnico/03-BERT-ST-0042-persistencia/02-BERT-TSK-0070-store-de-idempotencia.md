---
id: BERT-TSK-0070
title: "TSK-0070 — Tabela idempotency_keys por schema e IIdempotencyStore"
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
- '[[00-BERT-ST-0042-persistencia]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/03-BERT-ST-0042-persistencia/02-BERT-TSK-0070-store-de-idempotencia.md
description: "Chave de idempotência gravada no mesmo SaveChanges do caso de uso, em cada schema, com índice único e retenção de 24h."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 03-BERT-ST-0042-persistencia, 02-BERT-TSK-0070-store-de-idempotencia]
---

## Descrição

ADR-0004.

- `IIdempotencyStore` em `src/Application` (abstração); implementação em Infrastructure.
- Tabela `idempotency_keys` **em cada schema**: chave (até 255), hash SHA-256 do corpo normalizado,
  status e corpo da resposta, id do recurso, `created_at`. **Índice único na chave.**
- A chave é **adicionada ao mesmo `DbContext`** do caso de uso — entra no mesmo `SaveChanges` da
  entidade e da outbox. Nunca um commit separado — quem grava é o `CommitAsync` do caso de uso.
- Leitura do registro existente para o replay.

O filtro HTTP e o comportamento `400/replay/422/409` são da TSK-0073.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

**Padrões .NET (§8.5):** a store só anexa a chave ao contexto; quem grava é o `CommitAsync` do Unit of Work do caso de uso.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/Abstractions/Idempotency/**
src/Infrastructure/Persistence/Idempotency/**
src/Infrastructure/Persistence/BookStore/Configurations/Idempotency/**
src/Infrastructure/Persistence/Fraud/Configurations/Idempotency/**
```

## Critério de Aceite

- [ ] Índice único na chave nos dois schemas
- [ ] A gravação não chama `SaveChanges` por conta própria
- [ ] Hash calculado sobre corpo normalizado (mesmo JSON com espaços diferentes gera o mesmo hash — teste)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~Idempotency"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 3 testes


## Notas de execução

### Decisões

- `IIdempotencyStore` (Find/Add) em `Application.Abstractions.Idempotency`; `IdempotencyHasher` (ComputeHash) como classe estática separada — hash não é responsabilidade do store.
- `IBookStoreIdempotencyStore` e `IFraudIdempotencyStore` como sub-interfaces para injeção tipada por contexto.
- Implementações (`IdempotencyStore<TContext>`) internas à Infrastructure; nunca chamam `SaveChangesAsync`.
- `IdempotencyEntry` mapeado nos dois DbContexts com índice único na coluna `Key`.
- Normalização JSON: re-serializa com `System.Text.Json` (compact, sem whitespace) antes de calcular SHA-256.

### Saída da prova

```
Aprovado!  – Com falha: 0, Aprovado: 7, Ignorado: 0, Total: 7, Duração: 138 ms
```

Checklist:
- [x] Índice único na chave nos dois schemas
- [x] A gravação não chama `SaveChanges` por conta própria
- [x] Hash calculado sobre corpo normalizado (7 testes)
