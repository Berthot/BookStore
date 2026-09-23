---
id: BERT-TSK-0079
title: "TSK-0079 — PurchaseBook e GetPurchase (POST/GET /api/v1/purchases)"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/05-BERT-ST-0044-marketplace/02-BERT-TSK-0079-compra-e-consulta.md
description: "Compra assíncrona via outbox com Idempotency-Key e consulta com customerMessage e fraudDetails."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 05-BERT-ST-0044-marketplace, 02-BERT-TSK-0079-compra-e-consulta]
---

## Descrição

- **PurchaseBook** (D-36, D-41): `Idempotency-Key` obrigatória (mesmo filtro da TSK-0073). Calcula o
  total, grava `Purchase` (`PendingFraudCheck`) + chave + `PurchasePlaced` na outbox **do schema
  `bookstore`, num único `CommitAsync`**. Responde `202` + `Location`. `404` se o livro não existe.
- **GetPurchase** (D-40): `status`, `customerMessage` e `fraudDetails` em campos separados.
  `customerMessage` é **igual** para `PENDING_FRAUD_CHECK` e `UNDER_REVIEW` ("Pagamento em análise.")
  — não revela a revisão. `fraudDetails` é `null` sem decisão.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/Sales/PurchaseBook/**
src/Application/UseCases/Sales/GetPurchase/**
apps/WebApi/Endpoints/Sales/**
tests/Tests.Application/UseCases/Sales/PurchaseBook/**
tests/Tests.Application/UseCases/Sales/GetPurchase/**
```

## Critério de Aceite

- [x] Compra toca só o schema `bookstore` na requisição
- [x] `customerMessage` idêntica em pendente e em revisão (teste)
- [x] `fraudDetails` separado de `customerMessage`

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~PurchaseBook|FullyQualifiedName~GetPurchase"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 5 testes


## Notas de execução

Implementados `PurchaseBookHandler` e `GetPurchaseHandler`. `PurchaseBook` usa `IBookStoreUnitOfWork`
exclusivamente (schema `bookstore`). `GetPurchase` retorna a mesma `customerMessage` ("Pagamento em
análise.") para `PENDING_FRAUD_CHECK` e `UNDER_REVIEW` — não expõe ao cliente que há revisão manual.
`fraudDetails` é `null` enquanto não há decisão.

**Saída da prova (2026-09-23):**
```
dotnet test tests/Tests.Application --filter "FullyQualifiedName~PurchaseBook|FullyQualifiedName~GetPurchase"

Aprovado Handle_customer_message_is_identical_for_pending_and_under_review
Aprovado Handle_fraud_details_is_null_for_pending_and_under_review
Aprovado Handle_maps_status_to_uppercase
Aprovado Handle_returns_approved_fraud_details_for_confirmed_purchase
Aprovado Handle_returns_not_found_when_purchase_does_not_exist
Aprovado Handle_returns_rejected_fraud_details_for_cancelled_purchase
Aprovado Handle_commits_once_on_success
Aprovado Handle_creates_purchase_with_correct_total
Aprovado Handle_does_not_commit_when_book_not_found
Aprovado Handle_publishes_purchase_placed_with_correct_data
Aprovado Handle_returns_not_found_when_book_does_not_exist
Aprovado Handle_returns_pending_fraud_check_status

Total de testes: 12  |  Aprovados: 12  |  Tempo total: 0,91 s
```
