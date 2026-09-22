---
id: BERT-TSK-0066
title: "TSK-0066 — IFraudRule, FraudContext, FraudRuleSet e DecisionPolicy"
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
- '[[00-BERT-ST-0041-dominio-do-antifraude]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/02-BERT-ST-0041-dominio-do-antifraude/03-BERT-TSK-0066-motor-de-regras.md
description: "O modelo do motor: interface da regra, contexto de sinais, conjunto que executa todas e política que converte score em Outcome."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 02-BERT-ST-0041-dominio-do-antifraude, 03-BERT-TSK-0066-motor-de-regras]
---

## Descrição

ADR-0008.

- `IFraudRule`: `Code`, `Version`, `RuleEvaluation Evaluate(FraudContext)`.
- `FraudContext` (objeto de valor): `Amount`, `Delivery`, `ItemCount`, `Channel`, `OccurredAt`,
  `IsNewCustomer`, `RecentTransactionsWithSameCard`, `CustomerAverageAmount`,
  `CustomerTransactionCount`, `RecentJustBelowThresholdCount`. **As regras só leem** — quem calcula é
  o caso de uso (D-16).
- `FraudRuleSet`: executa **todas** as regras, soma os pesos, devolve o `Assessment` com todas as
  avaliações.
- `DecisionPolicy`: `score ≥ RejectThreshold → Rejected`; `≥ ReviewThreshold → Review`; senão
  `Approved`. Limites como constantes nomeadas com `<summary>` explicando o valor.

Testes da política nas **fronteiras**: exatamente no limite, um abaixo, um acima.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
src/Domain/Rules/IFraudRule.cs
src/Domain/Rules/FraudContext.cs
src/Domain/Rules/FraudRuleSet.cs
src/Domain/Rules/DecisionPolicy.cs
tests/Tests.Shared/Mothers/FraudAnalysis/FraudContextMother.cs
tests/Tests.Domain/Rules/FraudRuleSetTests.cs
tests/Tests.Domain/Rules/DecisionPolicyTests.cs
```

## Critério de Aceite

- [ ] `FraudRuleSet` registra avaliação de todas as regras, inclusive as que não dispararam (teste)
- [ ] `DecisionPolicy` testada nas fronteiras dos dois limites
- [ ] Nenhuma regra ou contexto referencia Infrastructure/EF

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Domain --filter "FullyQualifiedName~DecisionPolicy|FullyQualifiedName~FraudRuleSet"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 6 testes


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
