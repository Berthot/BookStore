---
id: BERT-TSK-0087
title: "TSK-0087 — Fixture Testcontainers e teste de migrations"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/07-BERT-ST-0046-testes-integrados/01-BERT-TSK-0087-fixture-e-migrations.md
description: "PostgresContainerFixture (um container por execução) e o teste 3: migrations dos dois contextos nos schemas certos."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 07-BERT-ST-0046-testes-integrados, 01-BERT-TSK-0087-fixture-e-migrations]
---

## Descrição

`tests/Tests.Infrastructure/Fixtures/PostgresContainerFixture.cs`: sobe **um** PostgreSQL por
execução (`[SetUpFixture]`) e reaproveita entre os testes. Categorias `[Integration]` e `[Slow]`.
**Não usar EF InMemory** — não tem transação nem é relacional (D-45, doc oficial desaconselha).

Teste 3: aplica as migrations reais dos dois contextos num banco vazio e verifica cada tabela no schema
certo e os dois `__EFMigrationsHistory` separados.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
tests/Tests.Infrastructure/Fixtures/**
tests/Tests.Infrastructure/Persistence/MigrationsTests.cs
tests/Tests.Infrastructure/Tests.Infrastructure.csproj
Directory.Packages.props
```

## Critério de Aceite

- [x] Um container por execução (não por teste)
- [x] Tabelas nos schemas corretos; históricos separados
- [x] Nenhum uso de `UseInMemoryDatabase`

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Infrastructure --filter "TestCategory=Integration&FullyQualifiedName~Migrations"`
— **Diretório:** raiz do repositório, com Docker — **Esperado:** todos aprovados


## Notas de execução

`PostgresContainerFixture` implementada com `[SetUpFixture]` — um único container PostgreSQL 17-alpine
por execução de assembly. `ConnectionString` é vazia quando Docker não disponível; todos os testes
de integração verificam e chamam `Assert.Ignore()` nesse caso.

`MigrationsTests` verifica: schemas `bookstore` e `fraud` criados; tabelas nos schemas corretos;
`__EFMigrationsHistory` separados (um por schema); tabelas não vazaram entre schemas.

**Saída da prova (2026-09-23):**
```
dotnet test tests/Tests.Infrastructure --filter "TestCategory=Integration"

Aprovado Assessments_table_is_in_fraud_schema
Aprovado Books_table_is_in_bookstore_schema
Aprovado Books_table_is_NOT_in_fraud_schema
Aprovado BookStore_migrations_history_is_in_bookstore_schema
Aprovado BookStore_schema_exists
Aprovado Fraud_migrations_history_is_in_fraud_schema
Aprovado Fraud_schema_exists
Aprovado IdempotencyKeys_table_is_in_bookstore_schema
Aprovado IdempotencyKeys_table_is_in_fraud_schema
Aprovado Purchases_table_is_in_bookstore_schema
Aprovado Transactions_table_is_in_fraud_schema
Aprovado Transactions_table_is_NOT_in_bookstore_schema
[+ 4 testes de idempotência e concorrência — ver TSK-0088/0089]

Total de testes: 16  |  Aprovados: 16  |  Tempo total: 8,13 s
```
