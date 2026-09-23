---
id: BERT-TSK-0074
title: "TSK-0074 — SubmitTransaction e GetTransaction (POST e GET /api/v1/transactions)"
type: task
versão: "1.0.0"
status: em-andamento
executor: claude-code
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-22
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

- [ ] POST responde `202` com `Location` e `status: RECEIVED`
- [ ] Handler do Submit chama `CommitAsync` uma única vez (teste)
- [ ] GET devolve `404` para id inexistente
- [ ] Corpo do GET não contém `correlationId`

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~SubmitTransaction|FullyQualifiedName~GetTransaction"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 5 testes


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
