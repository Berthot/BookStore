---
id: BERT-TSK-0084
title: "TSK-0084 — Aspire AppHost para desenvolvimento"
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
- '[[00-BERT-ST-0045-execucao-e-demo]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/06-BERT-ST-0045-execucao-e-demo/02-BERT-TSK-0084-apphost-aspire.md
description: "apps/AppHost orquestrando PostgreSQL, RabbitMQ, WebApi e Worker com as mesmas chaves de configuração do compose."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 06-BERT-ST-0045-execucao-e-demo, 02-BERT-TSK-0084-apphost-aspire]
---

## Descrição

`apps/AppHost` na solution (D-28, D-29). Papel: experiência de desenvolvimento; o compose continua
sendo o contrato de execução. **As aplicações leem as mesmas chaves** nos dois modos — só os valores
mudam. Se o AppHost exigir uma chave diferente, adapte o AppHost, nunca a aplicação.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
apps/AppHost/**
BookStore.slnx
Directory.Packages.props
```

## Critério de Aceite

- [x] `dotnet run --project apps/AppHost` sobe o ambiente e o painel
- [x] Nenhuma alteração em WebApi/Worker para acomodar o AppHost
- [x] `.slnx` relido antes de editar

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet build apps/AppHost -c Release`
— **Diretório:** raiz do repositório — **Esperado:** código de saída 0


## Notas de execução

`apps/AppHost` criado com `dotnet new aspire-apphost`. Orquestra PostgreSQL, RabbitMQ, WebApi e Worker.
As mesmas chaves de configuração (`ConnectionStrings__Default`, `ConnectionStrings__Messaging`,
`OTEL_EXPORTER_OTLP_ENDPOINT`) são usadas pelo AppHost e pelo compose — apenas os valores diferem.
Nenhuma alteração nas aplicações para acomodar o AppHost.

**Saída da prova (2026-09-23):**
```
dotnet build apps/AppHost -c Release

Build succeeded.
  0 Aviso(s)
  0 Erro(s)
```
