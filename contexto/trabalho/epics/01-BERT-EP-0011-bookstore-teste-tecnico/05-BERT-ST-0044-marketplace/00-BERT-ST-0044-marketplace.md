---
id: BERT-ST-0044
title: "ST-0044 — Marketplace: catálogo, compra e reação à decisão"
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
- '[[01-BERT-TSK-0078-catalogo]]'
- '[[02-BERT-TSK-0079-compra-e-consulta]]'
- '[[03-BERT-TSK-0080-porta-para-o-antifraude]]'
- '[[04-BERT-TSK-0081-reacao-a-decisao]]'
- '[[05-BERT-TSK-0082-reconciliacao-e-limpeza]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/05-BERT-ST-0044-marketplace/00-BERT-ST-0044-marketplace.md
description: "O marketplace de livros consumindo o antifraude: catálogo, compra assíncrona via outbox, porta para o antifraude, reação ao evento de decisão, reconciliação e limpeza de chaves."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 05-BERT-ST-0044-marketplace, 00-BERT-ST-0044-marketplace]
---

## Por que esta story existe

Mostra os contextos se conectando de ponta a ponta sem acoplamento: o marketplace só fala com o
antifraude por uma porta e por mensagens. Fluxo 2 de `docs/diagramas/02-sequencia.md`.

**Só existe venda com `APPROVED`.**

## Ordem

TSK-0078 → TSK-0079 → TSK-0080 → TSK-0081 → TSK-0082.

## Tasks

| Task | Título |
| :--- | :--- |
| `BERT-TSK-0078` | ListBooks (GET /api/v1/books) |
| `BERT-TSK-0079` | PurchaseBook e GetPurchase (POST/GET /api/v1/purchases) |
| `BERT-TSK-0080` | IFraudCheckGateway e consumidor de PurchasePlaced |
| `BERT-TSK-0081` | Consumidor de TransactionDecided: destino da compra |
| `BERT-TSK-0082` | Reconciliação e limpeza de chaves |
