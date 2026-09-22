---
id: BERT-ST-0040
title: "ST-0040 — Fundação da solução"
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
- '[[01-BERT-TSK-0060-referencias-e-build]]'
- '[[02-BERT-TSK-0061-projetos-de-teste-do-padrao]]'
- '[[03-BERT-TSK-0062-composicao-das-aplicacoes]]'
- '[[04-BERT-TSK-0063-telemetria-e-correlacao]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/01-BERT-ST-0040-fundacao-da-solucao/00-BERT-ST-0040-fundacao-da-solucao.md
description: "Deixa a solução compilando com as referências certas entre camadas, os projetos de teste do padrão e a composição das aplicações (DI segmentada, JSON, erros, Swagger, telemetria, correlação). Nenhuma regra de negócio aqui — é o chão onde o resto pisa."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 01-BERT-ST-0040-fundacao-da-solucao, 00-BERT-ST-0040-fundacao-da-solucao]
---

## Por que esta story existe

Hoje os projetos existem, mas não há referências entre eles, não há `Tests.Shared` nem
`Tests.Domain`, e as aplicações são templates. Toda task seguinte assume essa base; construí-la
primeiro evita que cada task invente a sua.

## Ordem

TSK-0060 → TSK-0061 → TSK-0062 → TSK-0063 (as duas últimas tocam o `Program.cs`; nunca em paralelo).

## Tasks

| Task | Título |
| :--- | :--- |
| `BERT-TSK-0060` | Referências entre projetos, build central e git |
| `BERT-TSK-0061` | Tests.Shared, Tests.Domain e o padrão de testes |
| `BERT-TSK-0062` | Composição: DI segmentada, JSON, erros, versão e Swagger |
| `BERT-TSK-0063` | Telemetria (OpenTelemetry) e X-Correlation-Id |
