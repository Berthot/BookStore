---
id: BERT-TSK-0072
title: "TSK-0072 — Migrations pela CLI, aplicação no startup e seed de livros"
type: task
versão: "1.0.0"
status: concluido
executor: claude-code
tags:
- '#contexto/bertho'
- '#area/trabalho'
created_at: 2026-09-22
updated_at: 2026-09-22
governed_by:
- '[[00-BERT-ST-0042-persistencia]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/03-BERT-ST-0042-persistencia/04-BERT-TSK-0072-migrations-e-seed.md
description: "Migrations iniciais dos dois contextos via dotnet ef, MigrateAsync no startup do WebApi atrás de flag e livros de exemplo."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 03-BERT-ST-0042-persistencia, 04-BERT-TSK-0072-migrations-e-seed]
---

## Descrição

- Gerar **pela CLI**, uma por contexto:

```bash
dotnet ef migrations add Initial --context FraudDbContext --project src/Infrastructure --startup-project apps/WebApi --output-dir Persistence/Fraud/Migrations
dotnet ef migrations add Initial --context BookStoreDbContext --project src/Infrastructure --startup-project apps/WebApi --output-dir Persistence/BookStore/Migrations
```

- WebApi aplica as pendentes no startup com `await db.Database.MigrateAsync(ct)` **direto** — nunca
  dentro de transação explícita (quebra o lock do EF Core 9+). Ligado por
  `Database:ApplyMigrationsOnStartup`. **O Worker não migra** (D-25).
- Seed idempotente de livros cobrindo os cenários: físico barato, e-book caro, físico de preço médio.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

**Padrões .NET (§8.10):** seed com `UseAsyncSeeding` (EF Core 9+) num `CatalogDataSeeder` idempotente, ligado por `SeedingOptions.Enabled`; a flag de migrations também é uma classe `*Options` validada no start.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Infrastructure/Persistence/BookStore/Migrations/**
src/Infrastructure/Persistence/Fraud/Migrations/**
src/Infrastructure/Persistence/Seed/**
apps/WebApi/Program.cs
apps/WebApi/appsettings*.json
src/Infrastructure/Options/**
```

## Critério de Aceite

- [x] Pastas `Migrations/` geradas pela CLI (sem edição manual)
- [x] Nenhuma pendência de modelo nos dois contextos
- [x] Worker não chama `Migrate`
- [x] Seed não duplica livros ao rodar duas vezes
- [x] `SeedingOptions` e a opção de migrations validadas com `ValidateOnStart()`

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet ef migrations has-pending-model-changes --context FraudDbContext --project src/Infrastructure --startup-project apps/WebApi`
— **Diretório:** raiz do repositório — **Esperado:** mensagem de que não há mudanças pendentes, código de saída 0

**Comando:** `dotnet ef migrations has-pending-model-changes --context BookStoreDbContext --project src/Infrastructure --startup-project apps/WebApi`
— **Diretório:** raiz do repositório — **Esperado:** idem


## Notas de execução

- Migrations geradas via CLI (`dotnet ef migrations add Initial`) para `FraudDbContext` e `BookStoreDbContext` — sem edição manual.
- `PaymentFingerprint` adicionado como propriedade first-class em `Transaction` para contornar limitação do EF Core 10: não é possível criar índice composto navegando por `ComplexProperty` via lambda nem via string path dotted.
- `DatabaseOptions` e `SeedingOptions` registrados com `ValidateOnStart()` em `PostgresExtensions.AddPersistence`.
- `UseAsyncSeeding` configurado no `AddPostgresDbContext<BookStoreDbContext>` — chama `CatalogDataSeeder` internamente; seed idempotente via `AnyAsync`.
- `ICatalogSeeder` público; `CatalogDataSeeder` interno; `Program.cs` resolve via DI e chama `SeedAsync` após `MigrateAsync`.
- Worker (`apps/Worker/Program.cs`) não chama `MigrateAsync`.

**Prova FraudDbContext:**
```
No changes have been made to the model since the last migration.
```

**Prova BookStoreDbContext:**
```
No changes have been made to the model since the last migration.
```
