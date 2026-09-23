---
id: BERT-TSK-0081
title: "TSK-0081 — Consumidor de TransactionDecided: destino da compra"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/05-BERT-ST-0044-marketplace/04-BERT-TSK-0081-reacao-a-decisao.md
description: "Atualiza a Purchase conforme a decisão: Confirmed, Cancelled ou UnderReview."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 05-BERT-ST-0044-marketplace, 04-BERT-TSK-0081-reacao-a-decisao]
---

## Descrição

No Worker (D-31). `APPROVED → CONFIRMED`, `REJECTED → CANCELLED`, `REVIEW → UNDER_REVIEW`; de
`UNDER_REVIEW`, a decisão do revisor leva a `CONFIRMED` ou `CANCELLED`.

Idempotente pela inbox **e** pelo estado: se a compra já está no estado final, não faz nada. Incrementa
`bookstore.purchases`.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/Sales/ApplyFraudDecision/**
apps/Worker/Consumers/Sales/TransactionDecidedConsumer.cs
tests/Tests.Application/UseCases/Sales/ApplyFraudDecision/**
```

## Critério de Aceite

- [x] Três resultados mapeados (teste por resultado)
- [x] Evento repetido não muda compra já finalizada (teste)
- [x] Revisão aprovada leva `UNDER_REVIEW` a `CONFIRMED` (teste)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~ApplyFraudDecision"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 4 testes


## Notas de execução

Implementado `ApplyFraudDecisionHandler` em `src/Application/UseCases/Sales/ApplyFraudDecision/`.
Consumidor `TransactionDecidedConsumer` no Worker. Três mapeamentos: `APPROVED → CONFIRMED`,
`REJECTED → CANCELLED`, `REVIEW → UNDER_REVIEW`. Estado final já atingido: handler retorna sucesso
sem alterar a compra (idempotência de domínio, além da inbox MassTransit).

Métrica `bookstore.purchases` incrementada com tag `status` em cada transição.

**Saída da prova (2026-09-23):**
```
dotnet test tests/Tests.Application --filter "FullyQualifiedName~ApplyFraudDecision"

Aprovado Handle_approved_sets_status_to_confirmed
Aprovado Handle_commits_once_on_successful_decision
Aprovado Handle_is_idempotent_when_purchase_already_confirmed
Aprovado Handle_rejected_sets_status_to_cancelled
Aprovado Handle_returns_not_found_when_no_purchase_for_transaction
Aprovado Handle_review_sets_status_to_under_review
Aprovado Handle_under_review_can_be_confirmed_by_reviewer

Total de testes: 7  |  Aprovados: 7  |  Tempo total: 0,79 s
```
