---
id: BERT-ST-0045
title: "ST-0045 — Execução e demonstração"
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
- '[[01-BERT-TSK-0083-containers-e-compose]]'
- '[[02-BERT-TSK-0084-apphost-aspire]]'
- '[[03-BERT-TSK-0085-postman-e-http]]'
- '[[04-BERT-TSK-0086-readme-how-to-use]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/06-BERT-ST-0045-execucao-e-demo/00-BERT-ST-0045-execucao-e-demo.md
description: "Tudo sobe com um comando, os 11 cenários ficam prontos no Postman e o README ganha o How to use real."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 06-BERT-ST-0045-execucao-e-demo, 00-BERT-ST-0045-execucao-e-demo]
---

## Por que esta story existe

A empresa vai **clonar e executar** o projeto antes da apresentação. Projeto que não sobe na máquina
deles é pior que não ter código. Referência: `docs/adr/0007-deploy.md`.

## Ordem

TSK-0083 → TSK-0084 → TSK-0085 → TSK-0086 (a última só depois de tudo verde).

## Tasks

| Task | Título |
| :--- | :--- |
| `BERT-TSK-0083` | Dockerfiles e docker-compose |
| `BERT-TSK-0084` | Aspire AppHost para desenvolvimento |
| `BERT-TSK-0085` | Collection do Postman com os cenários S1–S11 |
| `BERT-TSK-0086` | Preencher o How to use e os Testes do README |
