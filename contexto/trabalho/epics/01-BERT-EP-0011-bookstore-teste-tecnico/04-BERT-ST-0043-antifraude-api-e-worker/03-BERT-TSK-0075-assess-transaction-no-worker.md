---
id: BERT-TSK-0075
title: "TSK-0075 — AssessTransaction no Worker: sinais, regras e decisão"
type: task
versão: "1.0.0"
status: pendente
executor: claude-code
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-22
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

- [ ] Transação já decidida não é reavaliada (teste)
- [ ] Sinais são calculados no caso de uso e passados prontos ao contexto
- [ ] Decisão e evento gravados no mesmo `CommitAsync` (teste)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~AssessTransaction"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 4 testes


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
