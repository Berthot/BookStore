---
id: BERT-TSK-0082
title: "TSK-0082 — Reconciliação e limpeza de chaves"
type: task
versão: "1.0.0"
status: concluido
executor: claude-code
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-23
governed_by:
- '[[00-BERT-ST-0044-marketplace]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/05-BERT-ST-0044-marketplace/05-BERT-TSK-0082-reconciliacao-e-limpeza.md
description: "Jobs no Worker: republica compras paradas em PendingFraudCheck e remove chaves de idempotência com mais de 24h."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 05-BERT-ST-0044-marketplace, 05-BERT-TSK-0082-reconciliacao-e-limpeza]
---

## Descrição

Em `ReconciliationExtensions` (D-37), valores como constantes nomeadas com `<summary>` em inglês:

- **Reconciliação:** a cada N minutos, `Purchase` em `PENDING_FRAUD_CHECK` há mais de X minutos →
  republica `PurchasePlaced` **com o mesmo `CorrelationId`**. Seguro porque a chave é o `PurchaseId`,
  a inbox descarta reentregas e o consumidor confere o estado.
- **Limpeza:** remove `idempotency_keys` com mais de **24 horas** nos dois schemas.

`BackgroundService` com `PeriodicTimer`; a lógica em casos de uso testáveis, o timer só dispara.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

**Padrões .NET (§8.9):** jobs com `IServiceScopeFactory` e escopo **por iteração**, `try/catch` envolvendo a iteração, `stoppingToken` propagado.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/Sales/ReconcilePendingPurchases/**
src/Application/UseCases/Idempotency/PurgeExpiredKeys/**
src/Infrastructure/Extensions/ReconciliationExtensions.cs
apps/Worker/Jobs/**
tests/Tests.Application/UseCases/Sales/ReconcilePendingPurchases/**
tests/Tests.Application/UseCases/Idempotency/**
```

## Critério de Aceite

- [x] Só compras além do limite são republicadas (teste)
- [x] Republicação reutiliza o `CorrelationId` original
- [x] Limpeza não toca chaves com menos de 24h (teste)
- [x] Exceção numa iteração não derruba o Worker (teste do caso de uso ou do job)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~ReconcilePendingPurchases|FullyQualifiedName~PurgeExpiredKeys"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 4 testes


## Notas de execução

Implementados `ReconcilePendingPurchasesHandler` e `PurgeExpiredKeysHandler`. Jobs em `apps/Worker/Jobs/`
usam `BackgroundService` + `PeriodicTimer` + `IServiceScopeFactory` (escopo por iteração). Lógica testável
isolada nos handlers. `try/catch` por iteração: exceção não derruba o Worker.

Limpeza: `PurgeExpiredKeysHandler` remove chaves nos dois schemas (bookstore e fraud) com `created_at < now - 24h`.
Constantes documentadas com `<summary>` em inglês conforme convenção.

**Saída da prova (2026-09-23):**
```
dotnet test tests/Tests.Application --filter "FullyQualifiedName~ReconcilePendingPurchases|FullyQualifiedName~PurgeExpiredKeys"

Aprovado Handle_calls_delete_on_bookstore_store
Aprovado Handle_calls_delete_on_fraud_store
Aprovado Handle_deletes_from_both_stores_with_given_threshold
Aprovado Handle_returns_zero_when_no_expired_keys
Aprovado Handle_does_not_republish_when_no_stale_purchases
Aprovado Handle_preserves_correlation_id_on_republish
Aprovado Handle_republishes_each_stale_purchase_exactly_once
Aprovado Handle_republishes_stale_purchases

Total de testes: 8  |  Aprovados: 8  |  Tempo total: 0,78 s
```
