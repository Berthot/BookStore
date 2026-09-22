---
id: BERT-ST-0046
title: "ST-0046 — Testes integrados (extra, por último)"
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
- '[[01-BERT-TSK-0087-fixture-e-migrations]]'
- '[[02-BERT-TSK-0088-idempotencia-e-atomicidade]]'
- '[[03-BERT-TSK-0089-concorrencia]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/07-BERT-ST-0046-testes-integrados/00-BERT-ST-0046-testes-integrados.md
description: "Os quatro testes integrados em que um mock mentiria, com Testcontainers e um único container por execução."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 07-BERT-ST-0046-testes-integrados, 00-BERT-ST-0046-testes-integrados]
---

## Por que esta story existe

Regra da D-46: só é integrado se um mock mentiria. São quatro, e nenhum outro. **Só começa depois de
todas as outras stories concluídas** (D-08, D-47).

## Ordem

TSK-0087 → TSK-0088 → TSK-0089.

## Tasks

| Task | Título |
| :--- | :--- |
| `BERT-TSK-0087` | Fixture Testcontainers e teste de migrations |
| `BERT-TSK-0088` | Testes 1 e 2: índice único e atomicidade da outbox |
| `BERT-TSK-0089` | Teste 4: requisições iguais simultâneas |
