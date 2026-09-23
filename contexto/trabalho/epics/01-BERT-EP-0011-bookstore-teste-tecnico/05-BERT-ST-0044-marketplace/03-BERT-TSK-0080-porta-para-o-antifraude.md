---
id: BERT-TSK-0080
title: "TSK-0080 — IFraudCheckGateway e consumidor de PurchasePlaced"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/05-BERT-ST-0044-marketplace/03-BERT-TSK-0080-porta-para-o-antifraude.md
description: "Porta do Sales para o antifraude, adaptador em processo e consumidor que submete a transação com Idempotency-Key = PurchaseId."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 05-BERT-ST-0044-marketplace, 03-BERT-TSK-0080-porta-para-o-antifraude]
---

## Descrição

- `IFraudCheckGateway` **no contexto Sales** (D-26). O Sales não referencia nada de FraudAnalysis.
- Adaptador em processo que chama o caso de uso `SubmitTransaction` — trocá-lo por um cliente HTTP é a
  única mudança se o antifraude virar serviço.
- Consumidor de `PurchasePlaced` no Worker: traduz `BookFormat` → `DeliveryType` e quantidade →
  `ItemCount` (o antifraude não conhece livros, D-17), usa **`Idempotency-Key = PurchaseId`** e
  propaga o `CorrelationId` da compra. Grava `TransactionId` na `Purchase`.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/Sales/Ports/IFraudCheckGateway.cs
src/Infrastructure/Adapters/FraudCheckGateway.cs
src/Application/UseCases/Sales/SubmitPurchaseToFraud/**
apps/Worker/Consumers/Sales/PurchasePlacedConsumer.cs
tests/Tests.Application/UseCases/Sales/SubmitPurchaseToFraud/**
```

## Critério de Aceite

- [x] Nenhum tipo de `FraudAnalysis` é referenciado dentro de `Sales` (exceto no adaptador de Infrastructure)
- [x] Submeter a mesma compra duas vezes gera uma transação só (teste com a chave)
- [x] `EBOOK` vira `DIGITAL`

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~SubmitPurchaseToFraud"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 3 testes


## Notas de execução

`IFraudCheckGateway` definida em `src/Application/UseCases/Sales/Ports/`. Adaptador em
`src/Infrastructure/Adapters/FraudCheckGateway.cs` — chama `SubmitTransactionHandler` via mediator.
Nenhum namespace de `FraudAnalysis` é importado no projeto `Application` dentro do contexto `Sales`.

`EBOOK → DIGITAL`, `PHYSICAL_BOOK → PHYSICAL` na tradução. Chave de idempotência = `PurchaseId`.
Idempotência: segunda submissão retorna sucesso sem efeito colateral (gateway checa se `TransactionId` já
está vinculado à `Purchase`).

**Saída da prova (2026-09-23):**
```
dotnet test tests/Tests.Application --filter "FullyQualifiedName~SubmitPurchaseToFraud"

Aprovado Handle_calls_gateway_and_links_transaction
Aprovado Handle_commits_once_after_linking
Aprovado Handle_does_not_commit_when_already_idempotent
Aprovado Handle_is_idempotent_when_transaction_already_linked
Aprovado Handle_returns_not_found_when_purchase_missing

Total de testes: 5  |  Aprovados: 5  |  Tempo total: 0,74 s
```
