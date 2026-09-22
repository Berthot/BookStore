---
id: BERT-TSK-0085
title: "TSK-0085 — Collection do Postman com os cenários S1–S11"
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
- '[[00-BERT-ST-0045-execucao-e-demo]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/06-BERT-ST-0045-execucao-e-demo/03-BERT-TSK-0085-postman-e-http.md
description: "Collection em docs/postman com os 11 cenários em ordem, Idempotency-Key gerada por script e asserções."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 06-BERT-ST-0045-execucao-e-demo, 03-BERT-TSK-0085-postman-e-http]
---

## Descrição

`docs/postman/BookStore.postman_collection.json` + environment. Um request por passo, **na ordem**
S1..S11 (tabela do README). Pre-request gera `Idempotency-Key` (UUID v4); o S6 **repete a chave de
propósito**; o S7 repete a chave com outro corpo. Cada request tem teste (status e campos-chave).
Espera pela decisão assíncrona com polling curto no `GET` (limite de tentativas).

O S9 (parar o Worker) é **manual** — documente os passos na descrição do request.

Atualize também `apps/WebApi/WebApi.http` com os mesmos cenários.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
docs/postman/**
apps/WebApi/WebApi.http
```

## Critério de Aceite

- [ ] 11 cenários, na ordem, com asserções
- [ ] S6 e S7 reutilizam a chave de propósito
- [ ] S9 descrito como passo manual

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `npx --yes newman run docs/postman/BookStore.postman_collection.json -e docs/postman/local.postman_environment.json`
— **Diretório:** raiz do repositório, com o compose no ar — **Esperado:** zero falhas de asserção (S9 excluído)


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
