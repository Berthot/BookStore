---
id: BERT-TSK-0074
title: "TSK-0074 — SubmitTransaction e GetTransaction (POST e GET /api/v1/transactions)"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/04-BERT-ST-0043-antifraude-api-e-worker/02-BERT-TSK-0074-submit-e-get-transaction.md
description: "Caso de uso e endpoints do contrato exigido: 202 + Location no POST, decisão e histórico no GET."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 04-BERT-ST-0043-antifraude-api-e-worker, 02-BERT-TSK-0074-submit-e-get-transaction]
---

## Descrição

`src/Application/UseCases/FraudAnalysis/{SubmitTransaction,GetTransaction}/` com
`Request`, `Response`, `Handler` (D-11).

- **Submit:** valida, grava `Transaction` (`Received`) + chave + `TransactionSubmitted` na outbox **num
  único `CommitAsync`**, responde `202 Accepted` + `Location: /api/v1/transactions/{id}`.
- **Get:** `status`, `decision` (vigente, com todas as regras) e `history`. Enquanto não há decisão,
  `decision` é `null`. **Sem `correlationId` no corpo** — só no header.
- Formato exatamente como `docs/api/contrato.md`: dinheiro `{value, currency}`, enums
  `SNAKE_UPPER`, datas ISO 8601 UTC, erros Problem Details.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/FraudAnalysis/SubmitTransaction/**
src/Application/UseCases/FraudAnalysis/GetTransaction/**
apps/WebApi/Endpoints/FraudAnalysis/TransactionEndpoints.cs
tests/Tests.Application/UseCases/FraudAnalysis/SubmitTransaction/**
tests/Tests.Application/UseCases/FraudAnalysis/GetTransaction/**
```

## Critério de Aceite

- [x] POST responde `202` com `Location` e `status: RECEIVED`
- [x] Handler do Submit chama `CommitAsync` uma única vez (teste)
- [x] GET devolve `404` para id inexistente
- [x] Corpo do GET não contém `correlationId`

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~SubmitTransaction|FullyQualifiedName~GetTransaction"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 5 testes


## Notas de execução

Implementados `SubmitTransactionRequest/Response/Handler` e `GetTransactionRequest/Response/Handler` em
`src/Application/UseCases/FraudAnalysis/`. Handler do Submit grava `Transaction` + chave de idempotência +
evento `TransactionSubmitted` na outbox em um único `CommitAsync`. Resposta 202 + `Location` header.
`GetTransaction` retorna `decision: null` enquanto não há decisão; `history` é lista de `Assessment`.
Corpo do GET não inclui `correlationId` — presente apenas no `X-Correlation-Id` de resposta.

Validators FluentValidation criados na mesma pasta dos handlers.

**Saída da prova (2026-09-23):**
```
dotnet test tests/Tests.Application --filter "FullyQualifiedName~SubmitTransaction|FullyQualifiedName~GetTransaction"

Aprovado Handle_decided_transaction_includes_decision_and_history
Aprovado Handle_existing_transaction_returns_response_without_correlation_id
Aprovado Handle_received_transaction_decision_is_null
Aprovado Handle_unknown_id_returns_not_found
Aprovado Handle_publish_is_called_before_commit
Aprovado Handle_valid_request_adds_transaction_and_publishes_event
Aprovado Handle_valid_request_calls_commit_async_exactly_once
Aprovado Handle_valid_request_returns_success_with_received_status
Aprovado Handle_zero_amount_returns_validation_failure_without_side_effects

Total de testes: 9  |  Aprovados: 9  |  Tempo total: 0,86 s
```
