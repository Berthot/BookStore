---
id: BERT-TSK-0087
title: "TSK-0087 — Fixture Testcontainers e teste de migrations"
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

- [ ] Um container por execução (não por teste)
- [ ] Tabelas nos schemas corretos; históricos separados
- [ ] Nenhum uso de `UseInMemoryDatabase`

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Infrastructure --filter "TestCategory=Integration&FullyQualifiedName~Migrations"`
— **Diretório:** raiz do repositório, com Docker — **Esperado:** todos aprovados


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
