---
id: BERT-ST-0041
title: "ST-0041 — Domínio: marketplace e antifraude"
type: story
versão: "1.0.0"
status: pendente
executor: claude-code
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-22
governed_by:
- '[[00-BERT-EP-0011-bookstore-teste-tecnico]]'
children:
- '[[01-BERT-TSK-0064-objetos-de-valor-book-e-purchase]]'
- '[[02-BERT-TSK-0065-transaction-e-invariantes]]'
- '[[03-BERT-TSK-0066-motor-de-regras]]'
- '[[04-BERT-TSK-0067-regras-transacionais]]'
- '[[05-BERT-TSK-0068-regras-de-discrepancia]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/02-BERT-ST-0041-dominio-do-antifraude/00-BERT-ST-0041-dominio-do-antifraude.md
description: "Entidades, objetos de valor, invariantes e as seis regras antifraude — puro, sem infraestrutura, com testes unitários provando cada regra de negócio."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 02-BERT-ST-0041-dominio-do-antifraude, 00-BERT-ST-0041-dominio-do-antifraude]
---

## Por que esta story existe

É o coração do teste e a parte mais fácil de provar. Tudo aqui roda em milissegundos, sem banco,
sem fila. Cada invariante e cada regra tem teste próprio — é o que sustenta a frase da apresentação:
*"minhas regras de negócio têm testes que rodam sem infraestrutura"*.

Referências: `docs/diagramas/03-entidades.md` (modelo, invariantes I-01..I-07, regras),
`docs/diagramas/04-estados.md`, `docs/adr/0008-regras-antifraude.md`.

## Ordem

TSK-0064 → TSK-0065 → TSK-0066 → (TSK-0067 e TSK-0068 em paralelo).

## Tasks

| Task | Título |
| :--- | :--- |
| `BERT-TSK-0064` | Objetos de valor, Book e Purchase (ciclo de vida) |
| `BERT-TSK-0065` | Transaction, Assessment e as invariantes I-01..I-07 |
| `BERT-TSK-0066` | IFraudRule, FraudContext, FraudRuleSet e DecisionPolicy |
| `BERT-TSK-0067` | Regras transacionais: HighValueDigital, NewCustomerHighAmount, BulkQuantity, CardVelocity |
| `BERT-TSK-0068` | Regras de discrepância: AmountDeviation e Structuring |
