---
id: BERT-TSK-0081
title: "TSK-0081 — Consumidor de TransactionDecided: destino da compra"
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
- '[[00-BERT-ST-0044-marketplace]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/05-BERT-ST-0044-marketplace/04-BERT-TSK-0081-reacao-a-decisao.md
description: "Atualiza a Purchase conforme a decisão: Confirmed, Cancelled ou UnderReview."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 05-BERT-ST-0044-marketplace, 04-BERT-TSK-0081-reacao-a-decisao]
---

## Descrição

No Worker (D-31). `APPROVED → CONFIRMED`, `REJECTED → CANCELLED`, `REVIEW → UNDER_REVIEW`; de
`UNDER_REVIEW`, a decisão do revisor leva a `CONFIRMED` ou `CANCELLED`.

Idempotente pela inbox **e** pelo estado: se a compra já está no estado final, não faz nada. Incrementa
`bookstore.purchases`.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/Sales/ApplyFraudDecision/**
apps/Worker/Consumers/Sales/TransactionDecidedConsumer.cs
tests/Tests.Application/UseCases/Sales/ApplyFraudDecision/**
```

## Critério de Aceite

- [ ] Três resultados mapeados (teste por resultado)
- [ ] Evento repetido não muda compra já finalizada (teste)
- [ ] Revisão aprovada leva `UNDER_REVIEW` a `CONFIRMED` (teste)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~ApplyFraudDecision"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 4 testes


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
