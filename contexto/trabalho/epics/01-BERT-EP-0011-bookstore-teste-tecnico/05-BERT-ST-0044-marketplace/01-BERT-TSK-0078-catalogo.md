---
id: BERT-TSK-0078
title: "TSK-0078 — ListBooks (GET /api/v1/books)"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/05-BERT-ST-0044-marketplace/01-BERT-TSK-0078-catalogo.md
description: "Caso de uso e endpoint do catálogo."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 05-BERT-ST-0044-marketplace, 01-BERT-TSK-0078-catalogo]
---

## Descrição

`src/Application/UseCases/Catalog/ListBooks/` (Request, Response, Handler) e o endpoint
`GET /api/v1/books` conforme o contrato (preço `{value, currency}`, `format` em `SNAKE_UPPER`).

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/UseCases/Catalog/ListBooks/**
apps/WebApi/Endpoints/Catalog/**
tests/Tests.Application/UseCases/Catalog/ListBooks/**
```

## Critério de Aceite

- [ ] Endpoint responde a lista no formato do contrato
- [ ] Handler testado com repositório substituído

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~ListBooks"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 2 testes


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
