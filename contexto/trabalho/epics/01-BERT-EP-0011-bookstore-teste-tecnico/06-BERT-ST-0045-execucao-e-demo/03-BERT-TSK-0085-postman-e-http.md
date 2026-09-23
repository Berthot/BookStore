---
id: BERT-TSK-0085
title: "TSK-0085 — Collection do Postman com os cenários S1–S11"
type: task
versão: "1.0.0"
status: concluido
executor: claude-code
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-23
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

- [x] 11 cenários, na ordem, com asserções
- [x] S6 e S7 reutilizam a chave de propósito
- [x] S9 descrito como passo manual

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `npx --yes newman run docs/postman/BookStore.postman_collection.json -e docs/postman/local.postman_environment.json`
— **Diretório:** raiz do repositório, com o compose no ar — **Esperado:** zero falhas de asserção (S9 excluído)


## Notas de execução

Collection criada em `docs/postman/BookStore.postman_collection.json` com environment
`docs/postman/local.postman_environment.json`. Os 11 cenários S1–S11 na ordem com asserções
de status e campos-chave em cada request. Pre-request script gera `Idempotency-Key` como UUID v4.

S6 reutiliza a chave do S3 propositalmente → espera 200 replay. S7 reutiliza a mesma chave com
corpo diferente → espera 422. S9 (parar Worker) descrito como passo manual na descrição do request.

`apps/WebApi/WebApi.http` atualizado com os mesmos cenários.
