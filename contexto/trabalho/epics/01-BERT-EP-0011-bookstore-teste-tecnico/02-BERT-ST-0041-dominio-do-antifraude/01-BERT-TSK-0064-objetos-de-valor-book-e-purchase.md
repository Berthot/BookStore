---
id: BERT-TSK-0064
title: "TSK-0064 — Objetos de valor, Book e Purchase (ciclo de vida)"
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
- '[[00-BERT-ST-0041-dominio-do-antifraude]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/02-BERT-ST-0041-dominio-do-antifraude/01-BERT-TSK-0064-objetos-de-valor-book-e-purchase.md
description: "Money, PaymentInstrument, Decider e enums; Book (Catalog) e Purchase (Sales) com o ciclo PendingFraudCheck → Confirmed/Cancelled/UnderReview."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 02-BERT-ST-0041-dominio-do-antifraude, 01-BERT-TSK-0064-objetos-de-valor-book-e-purchase]
---

## Descrição

Em `src/Domain/Entities/<Contexto>/` (Catalog, Sales, FraudAnalysis) e um lugar para os objetos de
valor compartilhados:

- `Money` (valor `decimal` + moeda ISO 4217; valor > 0 onde a regra exigir), `PaymentInstrument`
  (`Type`, `Fingerprint`, `Last4?` — nunca o número do cartão), `Decider` (`Kind`, `ReviewerId?`).
- Enums: `TransactionStatus` (`Received`, `Processing`, `Decided` — **não existe `Failed`**, D-48),
  `Outcome`, `DeliveryType`, `Channel`, `BookFormat`, `PurchaseStatus`.
- `Book` (Catalog): título, autor, `Money Price`, `BookFormat`.
- `Purchase` (Sales): `BookId`, `Quantity`, `Total`, `TransactionId?`, `CorrelationId`, `Status`.
  **Só chega a `Confirmed` com `Approved`** — não existe outro caminho (D-38). `Confirmed` e
  `Cancelled` são finais.
- Mothers + Builders em `tests/Tests.Shared/Mothers/<Contexto>/`, espelhando o domínio.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

**Padrões .NET (§8.4):** crie a classe base `Entity` (`Guid Id { get; init; }`) em `src/Domain/Bases/`; entidades `sealed`; `CreatedAt`/`UpdatedAt` em UTC.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Domain/Entities/Catalog/**
src/Domain/Entities/Sales/**
src/Domain/ValueObjects/**
src/Domain/Enums/**
tests/Tests.Shared/Mothers/Catalog/**
tests/Tests.Shared/Mothers/Sales/**
tests/Tests.Domain/Entities/Catalog/**
tests/Tests.Domain/Entities/Sales/**
src/Domain/Bases/**
```

## Critério de Aceite

- [x] `Purchase` só vai a `Confirmed` a partir de `Approved`; tentativas a partir de outro resultado falham (teste)
- [x] `Confirmed` e `Cancelled` não mudam de estado (teste)
- [x] `UnderReview` vai a `Confirmed` ou `Cancelled` (teste)
- [x] Nenhum teste instancia entidade sem Mother/Builder
- [x] `TransactionStatus` não tem `Failed`
- [x] Entidades herdam de `Entity` com `Guid Id { get; init; }`

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Domain --filter "FullyQualifiedName~Purchase"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 5 testes


## Notas de execução

- Criados: `Entity`, `DomainError`, todos os enums (6), VOs (`Money`, `PaymentInstrument`, `Decider`), entidades `Book` e `Purchase` com ciclo de vida completo.
- `PurchaseBuilder.Build()` não define `Status` explicitamente — `PurchaseStatus.PendingFraudCheck` é o valor default do enum (0), evitando set em `private set`.
- Mothers `PurchaseMother.Confirmed/Cancelled/UnderReview()` constroem via `Build()` + `ApplyDecision()`.
- `Tests.Shared.csproj` recebeu `ProjectReference` ao `Domain` (necessário para as Mothers).
- Prova: `dotnet test tests/Tests.Domain --filter "FullyQualifiedName~Purchase"` → **10 aprovados**.
- Build completo: `dotnet build BookStore.slnx -c Release` → 0 erros, 0 warnings.

- [x] `Purchase` só vai a `Confirmed` a partir de `Approved`; tentativas a partir de outro resultado falham (teste)
- [x] `Confirmed` e `Cancelled` não mudam de estado (teste)
- [x] `UnderReview` vai a `Confirmed` ou `Cancelled` (teste)
- [x] Nenhum teste instancia entidade sem Mother/Builder
- [x] `TransactionStatus` não tem `Failed`
- [x] Entidades herdam de `Entity` com `Guid Id { get; init; }`
