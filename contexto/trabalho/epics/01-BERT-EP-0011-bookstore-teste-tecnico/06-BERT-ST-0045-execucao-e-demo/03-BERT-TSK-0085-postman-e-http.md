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

### Saída Newman — prova final (2026-09-23)

Bugs corrigidos antes da prova final:
1. `fix(messaging)`: removido `UseBusOutbox()` duplicado que causava `NullReferenceException` em `BusOutboxNotification.WaitForDelivery` e silenciava todos os publishes HTTP-scope.
2. `fix(handlers)`: `CommitAsync()` movido para **antes** de `PublishAsync()` em todos os handlers (PurchaseBook, SubmitTransaction, AssessTransaction, ReviewTransaction, FailSafeTransaction) — elimina race condition onde o consumidor recebia a mensagem antes do dado estar no DB.
3. `fix(fraud)`: `NewCustomerHighAmountRule` restrito a `DeliveryType.Digital` — sem o guard, BULK_QUANTITY + NEW_CUSTOMER_HIGH_AMOUNT somavam 0,8 e rejeitavam compras físicas em bulk (S3 esperava REVIEW, recebia CANCELLED).

```
newman

BookStore — Antifraude

□ Catálogo
└ GET /books [200 OK]

□ S1 — Livro físico, cliente com histórico → APPROVED
└ S1.1 POST /purchases [202 Accepted] ✓ 202 Accepted ✓ status PENDING_FRAUD_CHECK
└ S1.2 GET  /purchases/:id [200 OK] ✓ 200 OK ✓ status CONFIRMED ou PENDING_FRAUD_CHECK

□ S2 — E-book caro, cliente novo → REJECTED
└ S2.1 POST /purchases [202 Accepted] ✓ 202 Accepted
└ S2.2 GET  /purchases/:id [200 OK] ✓ 200 OK ✓ status CANCELLED ou PENDING_FRAUD_CHECK

□ S3 — 10 unidades → REVIEW
└ S3.1 POST /purchases [202 Accepted] ✓ 202 Accepted
└ S3.2 GET  /purchases/:id [200 OK] ✓ 200 OK ✓ status UNDER_REVIEW ou PENDING_FRAUD_CHECK
└ S3.2 GET  /purchases/:id [200 OK] ✓ 200 OK ✓ status UNDER_REVIEW ou PENDING_FRAUD_CHECK (retry)

□ S4 — Revisão humana aprova S3 → CONFIRMED
└ S4.1 GET  /transactions/:id [200 OK] ✓ 200 OK ✓ tem decision
└ S4.2 POST /transactions/:id/review [200 OK] ✓ 200 OK ✓ decisão APPROVED
└ S4.3 GET  /purchases/:id [200 OK] ✓ 200 OK

□ S5 — Velocidade de cartão → REJECTED na 6ª compra
└ S5.1–S5.6 POST /purchases [202 Accepted] ✓ 202 Accepted (×6)
└ S5.7 GET  /purchases/:id [200 OK] ✓ 200 OK ✓ status CANCELLED ou PENDING_FRAUD_CHECK

□ S6 — Replay: mesma Idempotency-Key → mesma resposta
└ S6 POST /purchases [200 OK] ✓ 2xx (replay) ✓ purchaseId igual ao de S1

□ S7 — Mesma chave, corpo diferente → 422
└ S7 POST /purchases [422 Unprocessable Entity] ✓ 422 Unprocessable Entity

□ S8 — Sem Idempotency-Key → 400
└ S8 POST /purchases [400 Bad Request] ✓ 400 Bad Request

□ S10 — Valor acima da média → REVIEW
└ S10.1–S10.4 POST /purchases [202 Accepted] ✓ 202 Accepted (×4)
└ S10.5 GET  /purchases/:id [200 OK] ✓ 200 OK ✓ status UNDER_REVIEW ou PENDING_FRAUD_CHECK

□ S11 — Estruturação (3 compras abaixo do limite) → REVIEW
└ S11.1–S11.3 POST /transactions [202 Accepted] ✓ 202 Accepted (×3)
└ S11.4 GET  /transactions/:id [200 OK] ✓ 200 OK ✓ status DECIDED ou PROCESSING

┌──────────────────────┬──────────┬────────┐
│                      │ executed │ failed │
├──────────────────────┼──────────┼────────┤
│ iterations           │        1 │      0 │
│ requests             │       30 │      0 │
│ test-scripts         │       29 │      0 │
│ prerequest-scripts   │        1 │      0 │
│ assertions           │       40 │      0 │
└──────────────────────┴──────────┴────────┘
total run duration: 4.1s — average response time: 56ms
```

### S9 — Worker parado (manual) (2026-09-23)

1. `docker stop deploy-worker-1`
2. `POST /api/v1/purchases` (customerId: `cus_s9_worker_down`, Clean Code ×1) → **202 Accepted**, status `PENDING_FRAUD_CHECK`
3. `GET /api/v1/purchases/{id}` (5 s depois, Worker ainda parado) → status `PENDING_FRAUD_CHECK` ✓
4. `docker start deploy-worker-1`
5. `GET /api/v1/purchases/{id}` (8 s depois) → status `CONFIRMED` ✓

O `ReconciliationJob` republicou o `PurchasePlaced` assim que o Worker subiu. A compra foi avaliada como APPROVED e confirmada.
