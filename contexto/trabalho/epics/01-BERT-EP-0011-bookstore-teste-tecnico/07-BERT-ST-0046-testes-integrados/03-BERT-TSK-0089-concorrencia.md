---
id: BERT-TSK-0089
title: "TSK-0089 — Teste 4: requisições iguais simultâneas"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/07-BERT-ST-0046-testes-integrados/03-BERT-TSK-0089-concorrencia.md
description: "Duas requisições com a mesma chave ao mesmo tempo: uma aceita, a outra replay ou 409."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 07-BERT-ST-0046-testes-integrados, 03-BERT-TSK-0089-concorrencia]
---

## Descrição

Dispara duas execuções concorrentes com a mesma chave e o mesmo corpo. Resultado: **uma** transação
criada; a outra recebe replay (se a primeira confirmou) ou `409` (se o `lock_timeout` esgotou). Nunca
duas transações. É a prova da resposta à pergunta mais provável da banca.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
tests/Tests.Infrastructure/Persistence/IdempotencyConcurrencyTests.cs
```

## Critério de Aceite

- [ ] Rodado 20 vezes seguidas sem falha intermitente (anotar nas notas)
- [ ] Nunca duas transações para a mesma chave

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Infrastructure --filter "TestCategory=Integration&FullyQualifiedName~Concurrency"`
— **Diretório:** raiz do repositório, com Docker — **Esperado:** todos aprovados


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
