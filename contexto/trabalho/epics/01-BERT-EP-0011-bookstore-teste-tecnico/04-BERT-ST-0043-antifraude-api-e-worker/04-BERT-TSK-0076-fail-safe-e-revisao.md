---
id: BERT-TSK-0076
title: "TSK-0076 — Fail-safe e endpoint de revisão"
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
- '[[00-BERT-ST-0043-antifraude-api-e-worker]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/04-BERT-ST-0043-antifraude-api-e-worker/04-BERT-TSK-0076-fail-safe-e-revisao.md
description: "Ao esgotar tentativas, Review decidido pelo sistema; POST /transactions/{id}/review com revisor genérico."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 04-BERT-ST-0043-antifraude-api-e-worker, 04-BERT-TSK-0076-fail-safe-e-revisao]
---

## Descrição

**Fail-safe (D-37, I-07):** consumidor de `Fault<TransactionSubmitted>` — esgotadas as tentativas, a
transação recebe `FailSafe(...)`: `Assessment` com `Outcome = Review`, `DecidedBy = System`, motivo
"avaliação indisponível", e `TransactionDecided` é publicado. Nunca aprova às cegas.

**Revisão (D-49, D-50):** `POST /api/v1/transactions/{id}/review` com `outcome` (`APPROVED`/`REJECTED`),
`reviewerId` opcional, `justification` obrigatória.

- `404` inexistente; `409` se a vigente não é `Review` (inclusive segunda revisão — é o que torna o
  endpoint idempotente, **sem** `Idempotency-Key`); `422` justificativa vazia ou `outcome` inválido.
- Premissa: **um único revisor**. Qualquer `reviewerId` (ou vazio) é aceito e o `Assessment` grava
  um id genérico constante. Comentário `<summary>` em inglês registrando a premissa e que, em produção,
  o id viria do token.
- Resposta `200` com o mesmo corpo do `GET`. Publica `TransactionDecided` pela outbox.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/FraudAnalysis/ReviewTransaction/**
src/Application/UseCases/FraudAnalysis/FailSafe/**
apps/Worker/Consumers/FraudAnalysis/TransactionSubmittedFaultConsumer.cs
apps/WebApi/Endpoints/FraudAnalysis/ReviewEndpoints.cs
tests/Tests.Application/UseCases/FraudAnalysis/ReviewTransaction/**
tests/Tests.Application/UseCases/FraudAnalysis/FailSafe/**
```

## Critério de Aceite

- [x] Fail-safe só produz `Review` (teste)
- [x] Segunda revisão recebe `409` (teste)
- [x] Revisão sem justificativa recebe `422` (teste)
- [x] Revisão grava novo `Assessment`; o anterior fica no histórico

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~ReviewTransaction|FullyQualifiedName~FailSafe"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 5 testes


## Notas de execução

Implementados `ReviewTransactionHandler` e `FailSafeTransactionHandler`. O fail-safe nunca aprova:
sempre produz `Outcome = Review` com `DecidedBy = System`. O handler de revisão rejeita segunda revisão
com `409 Conflict` (domain rule: `Review_WhenCurrentOutcomeIsNotReview_ReturnsDomainError`). Histório
preservado: o `Assessment` anterior permanece em `transaction.History`.

Revisor genérico: `reviewerId` é opcional; internamente gravado como constante (premissa de revisor único
documentada em `<summary>` em inglês).

**Saída da prova (2026-09-23):**
```
dotnet test tests/Tests.Application --filter "FullyQualifiedName~ReviewTransaction|FullyQualifiedName~FailSafe"

Aprovado Handle_already_decided_returns_success_without_side_effects
Aprovado Handle_fail_safe_always_produces_review_outcome
Aprovado Handle_processing_transaction_commits_and_publishes_review
Aprovado Handle_empty_justification_returns_unprocessable
Aprovado Handle_invalid_outcome_returns_unprocessable
Aprovado Handle_transaction_not_pending_review_returns_conflict
Aprovado Handle_unknown_transaction_returns_not_found
Aprovado Handle_valid_review_records_assessment_and_keeps_history

Total de testes: 8  |  Aprovados: 8  |  Tempo total: 0,93 s
```
