---
id: BERT-TSK-0069
title: "TSK-0069 — BookStoreDbContext, FraudDbContext e configurations"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/03-BERT-ST-0042-persistencia/01-BERT-TSK-0069-dbcontexts-e-configurations.md
description: "Dois DbContexts com schema próprio, configurations por contexto, factories de design-time e AddPostgresDbContext genérico."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 03-BERT-ST-0042-persistencia, 01-BERT-TSK-0069-dbcontexts-e-configurations]
---

## Descrição

```
src/Infrastructure/Persistence/
  BookStore/  BookStoreDbContext.cs · BookStoreDbContextFactory.cs · Configurations/{Catalog,Sales}/
  Fraud/      FraudDbContext.cs · FraudDbContextFactory.cs · Configurations/FraudAnalysis/
```

- `HasDefaultSchema("bookstore")` / `HasDefaultSchema("fraud")`.
- `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")` em cada contexto.
- `ApplyConfigurationsFromAssembly(asm, t => t.Namespace?.StartsWith("<ns>.Persistence.Fraud") == true)`
  (idem BookStore). **Sem o filtro, um contexto mapeia as entidades do outro.**
- `IDesignTimeDbContextFactory` por contexto — a CLI não sobe o host inteiro.
- `PostgresExtensions.AddPostgresDbContext<TContext>(configuration, schema)`: **uma** extension serve aos
  dois contextos (connection string, schema, histórico, retry de conexão). `<summary>` em inglês.
- `RuleEvaluation` em **tabela própria** (sem `jsonb`, D-51); índice `(payment_fingerprint, occurred_at)`.
- `CorrelationId` persistido em `Transaction` e `Purchase`.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

**Padrões .NET (§8.5) — esta task cria o acesso a dados:**

- `IRepository<TEntity> where TEntity : Entity` e interfaces específicas (`ITransactionRepository`,
  `IPurchaseRepository`, `IBookRepository`) nas abstrações; implementações em
  `Persistence/<Db>/Repositories/`. **Repositório nunca chama `SaveChangesAsync`.**
- `IUnitOfWork` (`Task<bool> CommitAsync(CancellationToken)`) com derivadas `IBookStoreUnitOfWork` e
  `IFraudUnitOfWork`, uma por contexto.
- Registro explícito de cada repositório. Configuração de conexão via classe `*Options` validada no start (§8.6).

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Infrastructure/Persistence/**
src/Infrastructure/Extensions/PostgresExtensions.cs
src/Infrastructure/DependencyInjection.cs
tests/Tests.Infrastructure/Persistence/ModelTests.cs
src/Domain/Repositories/**
src/Infrastructure/Persistence/BookStore/Repositories/**
src/Infrastructure/Persistence/Fraud/Repositories/**
src/Infrastructure/Persistence/UnitOfWork/**
```

## Critério de Aceite

- [ ] Cada contexto só enxerga as entidades do seu schema (teste sobre o `Model`, sem banco)
- [ ] `RuleEvaluation` mapeado como tabela, não coluna JSON
- [ ] Índice `(payment_fingerprint, occurred_at)` declarado
- [ ] `AddPostgresDbContext<T>` usado pelos dois contextos
- [ ] Nenhum repositório chama `SaveChanges`/`SaveChangesAsync` (verificado por busca e anotado)
- [ ] `IBookStoreUnitOfWork` e `IFraudUnitOfWork` registrados separadamente

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Infrastructure --filter "FullyQualifiedName~ModelTests"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados; prova que `FraudDbContext` não contém `Book`/`Purchase` e vice-versa


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
