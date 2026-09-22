---
id: BERT-ST-0042
title: "ST-0042 — Persistência: dois contextos, idempotência e mensageria"
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
- '[[01-BERT-TSK-0069-dbcontexts-e-configurations]]'
- '[[02-BERT-TSK-0070-store-de-idempotencia]]'
- '[[03-BERT-TSK-0071-masstransit-outbox-inbox]]'
- '[[04-BERT-TSK-0072-migrations-e-seed]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/03-BERT-ST-0042-persistencia/00-BERT-ST-0042-persistencia.md
description: "Dois DbContexts em schemas separados, configurations por contexto, tabela de idempotência por schema, tabelas do MassTransit e migrations geradas pela CLI."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 03-BERT-ST-0042-persistencia, 00-BERT-ST-0042-persistencia]
---

## Por que esta story existe

As três garantias do desafio — idempotência, resiliência, auditabilidade — moram no banco (ADR-0003,
ADR-0004). Aqui elas ganham tabela, índice e transação.

## Regras que não se negociam

- Migrations **só** via `dotnet ef migrations add` — nunca escritas ou editadas à mão (D-24).
- Um `DbContext` por schema; configurations aplicadas com **filtro por namespace** (D-23).
- Layout `src/Infrastructure/Persistence/{BookStore,Fraud}/` (D-33).

## Ordem

TSK-0069 → TSK-0070 → TSK-0071 → TSK-0072.

## Tasks

| Task | Título |
| :--- | :--- |
| `BERT-TSK-0069` | BookStoreDbContext, FraudDbContext e configurations |
| `BERT-TSK-0070` | Tabela idempotency_keys por schema e IIdempotencyStore |
| `BERT-TSK-0071` | MassTransit: outbox, inbox, retry e DLQ |
| `BERT-TSK-0072` | Migrations pela CLI, aplicação no startup e seed de livros |
