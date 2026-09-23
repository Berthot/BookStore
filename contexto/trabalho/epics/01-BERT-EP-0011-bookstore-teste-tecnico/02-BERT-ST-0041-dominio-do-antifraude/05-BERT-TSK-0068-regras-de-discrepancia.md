---
id: BERT-TSK-0068
title: "TSK-0068 — Regras de discrepância: AmountDeviation e Structuring"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/02-BERT-ST-0041-dominio-do-antifraude/05-BERT-TSK-0068-regras-de-discrepancia.md
description: "Desvio de valor sobre a média do cliente e fracionamento logo abaixo de limite; ambas pesam para Review."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 02-BERT-ST-0041-dominio-do-antifraude, 05-BERT-TSK-0068-regras-de-discrepancia]
---

## Descrição

- `AmountDeviationRule` (`AMOUNT_DEVIATION`): valor muito acima da média do próprio cliente.
  **Só se aplica com histórico mínimo** (sem histórico, não dispara — o cliente novo é coberto por
  outra regra).
- `StructuringRule` (`STRUCTURING`): várias transações do cliente logo abaixo de um limite, em janela
  curta.

Ambas pesam para **`Review`**, não para rejeição: discrepância é sinal, não prova (ADR-0008).
Cenários S10 e S11.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
src/Domain/Rules/Discrepancy/**
tests/Tests.Domain/Rules/Discrepancy/**
```

## Critério de Aceite

- [x] `AmountDeviationRule` não dispara sem histórico mínimo (teste)
- [x] Cada regra tem teste de disparo e de não disparo
- [x] Sozinhas, cada uma leva a `Review`, nunca a `Rejected` (teste com a política)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Domain --filter "FullyQualifiedName~Rules.Discrepancy"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 5 testes


## Notas de execução

- AMOUNT_DEVIATION: requer CustomerTransactionCount ≥ 3 (MinHistoryCount) e Amount > 3x CustomerAverageAmount. Weight=0.4 → Review sozinha.
- STRUCTURING: requer RecentJustBelowThresholdCount ≥ 3. Weight=0.4 → Review sozinha.
- Ambas puras: só leem FraudContext, zero acesso a infra.
- Prova: `dotnet test tests/Tests.Domain --filter "FullyQualifiedName~Rules.Discrepancy"` → **7 aprovados**.

- [x] `AmountDeviationRule` não dispara sem histórico mínimo (teste)
- [x] Cada regra tem teste de disparo e de não disparo
- [x] Sozinhas, cada uma leva a `Review`, nunca a `Rejected` (teste com a política)
