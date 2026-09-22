---
id: BERT-TSK-0088
title: "TSK-0088 — Testes 1 e 2: índice único e atomicidade da outbox"
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
- '[[00-BERT-ST-0046-testes-integrados]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/07-BERT-ST-0046-testes-integrados/02-BERT-TSK-0088-idempotencia-e-atomicidade.md
description: "Mesma chave duas vezes gera uma transação; falha no SaveChanges não deixa entidade, chave nem mensagem."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 07-BERT-ST-0046-testes-integrados, 02-BERT-TSK-0088-idempotencia-e-atomicidade]
---

## Descrição

- **Teste 1:** mesma `Idempotency-Key` duas vezes → uma transação só; corpo diferente → barrado.
  Quem barra é o índice único do banco.
- **Teste 2:** força falha no `SaveChanges` → não sobra entidade, chave nem `OutboxMessage`.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
tests/Tests.Infrastructure/Persistence/IdempotencyTests.cs
tests/Tests.Infrastructure/Persistence/OutboxAtomicityTests.cs
```

## Critério de Aceite

- [ ] Teste 1 falha se o índice único for removido (verificado e anotado)
- [ ] Teste 2 confere as três tabelas após a falha

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Infrastructure --filter "TestCategory=Integration&(FullyQualifiedName~IdempotencyTests|FullyQualifiedName~OutboxAtomicity)"`
— **Diretório:** raiz do repositório, com Docker — **Esperado:** todos aprovados


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
