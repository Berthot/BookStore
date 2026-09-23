---
id: BERT-TSK-0075
title: "TSK-0075 — AssessTransaction no Worker: sinais, regras e decisão"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/04-BERT-ST-0043-antifraude-api-e-worker/03-BERT-TSK-0075-assess-transaction-no-worker.md
description: "Consumidor de TransactionSubmitted que calcula os sinais históricos, executa o motor e grava a decisão com o evento na outbox."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 04-BERT-ST-0043-antifraude-api-e-worker, 03-BERT-TSK-0075-assess-transaction-no-worker]
---

## Descrição

Consumidor `TransactionSubmitted` → caso de uso `AssessTransaction`:

1. **Checagem de estado:** se a transação já está `Decided`, não faz nada (idempotência no domínio,
   além da inbox).
2. `StartProcessing()` e **commit** — o `GET` passa a mostrar `PROCESSING`.
3. Monta o `FraudContext` com os sinais calculados **aqui** (velocidade por fingerprint, média e
   contagem do cliente, contagem logo abaixo do limite, cliente novo). As regras não consultam banco.
4. `FraudRuleSet` + `DecisionPolicy` → `Decide(...)` + `TransactionDecided` na outbox, **num único
   `CommitAsync`**.

Consultas de sinal usam o índice `(payment_fingerprint, occurred_at)`.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/FraudAnalysis/AssessTransaction/**
src/Infrastructure/Persistence/Fraud/Queries/**
apps/Worker/Consumers/FraudAnalysis/TransactionSubmittedConsumer.cs
tests/Tests.Application/UseCases/FraudAnalysis/AssessTransaction/**
```

## Critério de Aceite

- [x] Transação já decidida não é reavaliada (teste)
- [x] Sinais são calculados no caso de uso e passados prontos ao contexto
- [x] Decisão e evento gravados no mesmo `CommitAsync` (teste)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~AssessTransaction"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 4 testes


## Notas de execução

Implementado `AssessTransactionHandler` em `src/Application/UseCases/FraudAnalysis/AssessTransaction/`.
Consumidor `TransactionSubmittedConsumer` no Worker. Sinais históricos (velocidade, média, contagem de
itens abaixo do limite, cliente novo) consultados via `IFraudTransactionSignalRepository` em
`src/Infrastructure/Persistence/Fraud/Queries/`. O handler faz dois commits: primeiro `StartProcessing()`
(para o GET mostrar PROCESSING imediatamente), depois `Decide(...)` + evento na outbox.

Transação já `Decided` retorna sucesso sem efeitos colaterais (idempotência de domínio, além da inbox do MassTransit).

**Saída da prova (2026-09-23):**
```
dotnet test tests/Tests.Application --filter "FullyQualifiedName~AssessTransaction"

Aprovado Handle_already_decided_transaction_returns_success_without_side_effects
Aprovado Handle_publish_occurs_before_second_commit
Aprovado Handle_received_transaction_commits_twice_and_publishes_event
Aprovado Handle_signals_are_queried_from_repository
Aprovado Handle_unknown_transaction_returns_not_found

Total de testes: 5  |  Aprovados: 5  |  Tempo total: 1,08 s
```
