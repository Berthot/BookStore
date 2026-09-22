---
id: BERT-TSK-0071
title: "TSK-0071 — MassTransit: outbox, inbox, retry e DLQ"
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
- '[[00-BERT-ST-0042-persistencia]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/03-BERT-ST-0042-persistencia/03-BERT-TSK-0071-masstransit-outbox-inbox.md
description: "MassTransitExtensions com RabbitMQ, Bus Outbox e Consumer Inbox no EF Core dos dois contextos, retry com backoff e filas _error."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 03-BERT-ST-0042-persistencia, 03-BERT-TSK-0071-masstransit-outbox-inbox]
---

## Descrição

ADR-0002.

- Tabelas `InboxState`, `OutboxMessage`, `OutboxState` mapeadas **em cada** `DbContext`
  (`AddInboxStateEntity`, `AddOutboxMessageEntity`, `AddOutboxStateEntity`) — criadas pelas migrations
  normais, nada à mão.
- `MassTransitExtensions`: RabbitMQ como transporte, Bus Outbox (a mensagem entra no `SaveChanges`),
  Consumer Inbox, retry com backoff em intervalos crescentes, filas `_error` como DLQ.
- Intervalos e limites em **constantes nomeadas** com `<summary>` em inglês explicando o porquê
  (D-37). Ex.: *"Retry intervals grow to ride out transient database restarts without flooding the broker."*
- Mensagens: `PurchasePlaced`, `TransactionSubmitted`, `TransactionDecided`, com `CorrelationId`.
- Só esta extension conhece MassTransit — o domínio e a aplicação não.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
src/Infrastructure/Extensions/MassTransitExtensions.cs
src/Infrastructure/Messaging/**
src/Application/Messages/**
src/Infrastructure/Persistence/BookStore/BookStoreDbContext.cs
src/Infrastructure/Persistence/Fraud/FraudDbContext.cs
```

## Critério de Aceite

- [ ] Os dois contextos têm as três tabelas do MassTransit no modelo
- [ ] Nenhum arquivo de Domain/Application referencia `MassTransit`
- [ ] Toda constante de retry tem `<summary>` com o porquê

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet build BookStore.slnx -c Release`
— **Diretório:** raiz do repositório — **Esperado:** código de saída 0, zero avisos


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
