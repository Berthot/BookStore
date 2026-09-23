---
id: BERT-TSK-0065
title: "TSK-0065 — Transaction, Assessment e as invariantes I-01..I-07"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/02-BERT-ST-0041-dominio-do-antifraude/02-BERT-TSK-0065-transaction-e-invariantes.md
description: "Agregado Transaction com Assessments append-only, RuleEvaluation e as sete invariantes, cada uma com teste."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 02-BERT-ST-0041-dominio-do-antifraude, 02-BERT-TSK-0065-transaction-e-invariantes]
---

## Descrição

`Transaction` é a raiz do agregado e **contém** seus `Assessment` (D-03). `Assessment` é
append-only: a revisão humana gera um novo, o original fica (D-07). A decisão vigente é a mais
recente (`CurrentAssessment()`).

Comportamento, não setters: `StartProcessing()`, `Decide(Assessment)`, `Review(Assessment)`,
`FailSafe(string reason)`.

| # | Invariante |
| :--- | :--- |
| I-01 | `Amount.Value` > 0 |
| I-02 | `Received → Processing → Decided`; reentrar em `Processing` é permitido |
| I-03 | Assessment do motor só em `Processing` |
| I-04 | Assessment de revisor só com `Status = Decided` e vigente = `Review` |
| I-05 | Assessment de revisor exige `Justification` e não pode ser `Review` |
| I-06 | Assessments imutáveis; vigente = mais recente |
| I-07 | Assessment do sistema (fail-safe) só com `Outcome = Review` e só em `Processing` |

`RuleEvaluation` guarda `RuleCode`, `RuleVersion`, `Hit`, `Weight`, `Reason` — de **todas** as regras,
disparando ou não.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

**Padrões .NET (§8.4):** `Transaction` e `Assessment` herdam de `Entity`; `CreatedAt`/`UpdatedAt` UTC; estado só por métodos.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Domain/Entities/FraudAnalysis/Transaction.cs
src/Domain/Entities/FraudAnalysis/Assessment.cs
src/Domain/Entities/FraudAnalysis/RuleEvaluation.cs
tests/Tests.Shared/Mothers/FraudAnalysis/**
tests/Tests.Domain/Entities/FraudAnalysis/**
```

## Critério de Aceite

- [x] Cada invariante I-01..I-07 tem ao menos um teste com o id no nome ou no `[Description]`
- [x] Não há setter público para `Status` nem para a lista de assessments
- [x] Revisão gera novo `Assessment`; o anterior continua no histórico (teste)
- [x] Violação de invariante retorna erro de domínio explícito, não exceção genérica

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Domain --filter "FullyQualifiedName~Transaction"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 7 testes


## Notas de execução

- Criados: `RuleEvaluation` (record), `Assessment` (entity com factory methods ForEngine/ForReviewer/ForSystem), `Transaction` (aggregate root com StartProcessing/Decide/Review/FailSafe).
- `TransactionBuilder.BuildCreate()` invoca `Transaction.Create()` para testar I-01; `Build()` contorna o factory (status default Received).
- `Tests.Shared.csproj` já tem referência ao Domain (adicionada em TSK-0064).
- Prova: `dotnet test tests/Tests.Domain --filter "FullyQualifiedName~Transaction"` → **16 aprovados**.

- [x] Cada invariante I-01..I-07 tem ao menos um teste com o id no `[Description]`
- [x] Não há setter público para `Status` nem para a lista de assessments
- [x] Revisão gera novo `Assessment`; o anterior continua no histórico (teste)
- [x] Violação de invariante retorna erro de domínio explícito, não exceção genérica
