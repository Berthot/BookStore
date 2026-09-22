---
id: BERT-EP-0011
title: "EP-0011 — BookStore: implementação do teste técnico .NET"
type: epic
versão: "1.0.0"
status: pendente
executor: claude-code
references:
  - 05-matheus/05-brainstorm/03-ancora-teste-tecnico-dotnet.md
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-22
governed_by:
- '[[trabalho-bertho]]'
children:
- '[[00-BERT-ST-0040-fundacao-da-solucao]]'
- '[[00-BERT-ST-0041-dominio-do-antifraude]]'
- '[[00-BERT-ST-0042-persistencia]]'
- '[[00-BERT-ST-0043-antifraude-api-e-worker]]'
- '[[00-BERT-ST-0044-marketplace]]'
- '[[00-BERT-ST-0045-execucao-e-demo]]'
- '[[00-BERT-ST-0046-testes-integrados]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/00-BERT-EP-0011-bookstore-teste-tecnico.md
description: "Implementa o módulo antifraude e o marketplace de livros do teste técnico a partir do desenho fechado (ADRs, diagramas, contrato), com idempotência, resiliência e auditoria provadas em teste."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 00-BERT-EP-0011-bookstore-teste-tecnico]
---

## Por que este épico existe

Implementar o teste técnico **BookStore — Módulo Antifraude** a partir do desenho já fechado: 65
decisões, 4 diagramas, 8 ADRs e o contrato de API, todos em `docs/` do repositório. A empresa vai
**clonar, executar** e depois ouvir Bertho defender cada decisão — então o código precisa provar o que a
documentação afirma: **idempotente, resiliente e auditável**.

Âncora de origem (vault): `05-matheus/05-brainstorm/03-ancora-teste-tecnico-dotnet.md` (BERT-ANC-0005).

## O que muda quando fechar

- `docker compose -f deploy/docker-compose.yml up` sobe tudo na máquina de quem avalia.
- Os 11 cenários do README rodam no Swagger e no Postman com o resultado esperado.
- `dotnet test --filter "TestCategory=Unit"` passa sem Docker; `dotnet test` completo passa com Docker.
- Toda regra de negócio (invariantes I-01..I-07, seis regras antifraude, ciclo da compra) tem teste.

## Stories — em ordem

| # | Story | Tasks |
| :---: | :--- | :---: |
| 1 | `BERT-ST-0040` — Fundação da solução | 4 |
| 2 | `BERT-ST-0041` — Domínio: marketplace e antifraude | 5 |
| 3 | `BERT-ST-0042` — Persistência: dois contextos, idempotência e mensageria | 4 |
| 4 | `BERT-ST-0043` — Antifraude: API, Worker e revisão | 5 |
| 5 | `BERT-ST-0044` — Marketplace: catálogo, compra e reação à decisão | 5 |
| 6 | `BERT-ST-0045` — Execução e demonstração | 4 |
| 7 | `BERT-ST-0046` — Testes integrados (extra, por último) | 3 |

A ordem segue a D-08: fundação → domínio → persistência → antifraude → marketplace → execução →
testes integrados (extra, por último).

## Regras da rodada

- A documentação em `docs/` é a **fonte da verdade**. Se o código precisar divergir, **pare e
  reporte** — não altere `docs/` nem os ADRs por conta própria.
- Uma task por vez, commit por task, nunca `git add -A`. Nunca `git push` — push é do Bertho.
- Convenções completas: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`.
