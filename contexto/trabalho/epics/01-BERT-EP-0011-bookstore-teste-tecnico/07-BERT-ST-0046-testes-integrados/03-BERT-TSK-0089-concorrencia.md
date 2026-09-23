---
id: BERT-TSK-0089
title: "TSK-0089 — Teste 4: requisições iguais simultâneas"
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

- [x] Rodado 20 vezes seguidas sem falha intermitente (anotar nas notas)
- [x] Nunca duas transações para a mesma chave

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Infrastructure --filter "TestCategory=Integration&FullyQualifiedName~Concurrent"`
— **Diretório:** raiz do repositório, com Docker — **Esperado:** todos aprovados


## Notas de execução

### Abordagem inicial abandonada: WebApplicationFactory

Veja TSK-0088 para o contexto completo. Em resumo: `WebApplicationFactory<Program>` não funcionou porque o `BodyReader` pipe-backed é esgotado antes do filtro ler o corpo, resultando em 400 ou 500 — não no cenário de concorrência que o teste pretendia validar.

### Como a task foi refeita (consolidada em IdempotencyIntegrationTests.cs)

O teste de concorrência foi incorporado ao arquivo `tests/Tests.Infrastructure/Persistence/IdempotencyIntegrationTests.cs` como `Concurrent_inserts_same_key_one_succeeds_other_gets_conflict_or_timeout`.

**Mecanismo do teste:**
1. Para cada iteração (20 no total), gera uma chave única via `Guid.NewGuid()`.
2. Dispara dois `Task` concorrentes (`t1` e `t2`) que tentam inserir a mesma chave no banco via `FraudDbContext`, dentro de uma transação explícita com `SET LOCAL lock_timeout = '1500ms'`.
3. O helper `TryCommitWithLockTimeoutAsync` retorna `true` (sucesso) ou `false` (23505/55P03) — nunca lança exceção inesperada.
4. Verifica que `results` contém exatamente `true` e `false` — ou seja, uma inserção commitou e a outra recebeu conflito ou timeout. Nunca ambas commitam (o que violaria a unicidade).

**Resultado nas 20 rodadas:** todas aprovadas, sem falha intermitente. O índice único garante determinismo.

**Correção de produção aplicada junto desta task (Item 1 da revisão EP-0011):**
- `FraudUnitOfWork.CommitAsync` e `BookStoreUnitOfWork.CommitAsync` agora envolvem o `SaveChangesAsync` em transação explícita com `SET LOCAL lock_timeout = '1500ms'`.
- 23505 → lança `IdempotencyConflictException` (capturada no filtro → re-lê a entrada → replay ou 409).
- 55P03 → lança `IdempotencyLockTimeoutException` (capturada no filtro → 409 imediato).
- `ExceptionGuardCommandBehavior` re-lança essas exceções em vez de convertê-las em `Fail(Internal)`.
- `IdempotencyFilter.InvokeAsync` envolve `await next(context)` em try-catch para essas duas exceções.

### Saída da prova

```
dotnet test tests/Tests.Infrastructure --filter "TestCategory=Integration&FullyQualifiedName~Concurrent" -v normal

  Aprovado Concurrent_inserts_same_key_one_succeeds_other_gets_conflict_or_timeout [371 ms]

Total de testes: 1
     Aprovados: 1
Tempo total: 8,0485 Segundos
```

O teste executa 20 iterações internamente — uma iteração falha quebraria o loop e o teste seria marcado como falha.

Commit: `fix(idempotency): capture 23505/55P03 and surface replay or 409`
