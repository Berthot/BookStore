---
id: BERT-ST-0043
title: "ST-0043 — Antifraude: API, Worker e revisão"
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
- '[[01-BERT-TSK-0073-filtro-de-idempotencia]]'
- '[[02-BERT-TSK-0074-submit-e-get-transaction]]'
- '[[03-BERT-TSK-0075-assess-transaction-no-worker]]'
- '[[04-BERT-TSK-0076-fail-safe-e-revisao]]'
- '[[05-BERT-TSK-0077-metricas-de-negocio]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/04-BERT-ST-0043-antifraude-api-e-worker/00-BERT-ST-0043-antifraude-api-e-worker.md
description: "Contrato exigido pelo desafio (POST/GET /transactions) com idempotência, avaliação assíncrona no Worker, fail-safe, revisão humana e métricas."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 04-BERT-ST-0043-antifraude-api-e-worker, 00-BERT-ST-0043-antifraude-api-e-worker]
---

## Por que esta story existe

É o que o enunciado pede literalmente. Contrato em `docs/api/contrato.md`; fluxo em
`docs/diagramas/02-sequencia.md` (fluxo 1).

## Ordem

TSK-0073 → TSK-0074 → TSK-0075 → TSK-0076 → TSK-0077.

## Tasks

| Task | Título |
| :--- | :--- |
| `BERT-TSK-0073` | Filtro HTTP de idempotência (400, replay, 422, 409) |
| `BERT-TSK-0074` | SubmitTransaction e GetTransaction (POST e GET /api/v1/transactions) |
| `BERT-TSK-0075` | AssessTransaction no Worker: sinais, regras e decisão |
| `BERT-TSK-0076` | Fail-safe e endpoint de revisão |
| `BERT-TSK-0077` | Métricas de negócio e spans das regras |
