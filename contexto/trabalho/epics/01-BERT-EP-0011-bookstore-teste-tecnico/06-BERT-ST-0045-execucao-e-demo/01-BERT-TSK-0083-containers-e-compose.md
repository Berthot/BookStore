---
id: BERT-TSK-0083
title: "TSK-0083 — Dockerfiles e docker-compose"
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
- '[[00-BERT-ST-0045-execucao-e-demo]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/06-BERT-ST-0045-execucao-e-demo/01-BERT-TSK-0083-containers-e-compose.md
description: "Dockerfile multi-stage por aplicação e deploy/docker-compose.yml com healthchecks e Aspire Dashboard."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 06-BERT-ST-0045-execucao-e-demo, 01-BERT-TSK-0083-containers-e-compose]
---

## Descrição

- Dockerfile multi-stage para `apps/WebApi` e `apps/Worker` (build com SDK, imagem final só runtime).
- `deploy/docker-compose.yml`: PostgreSQL, RabbitMQ (com management), WebApi, Worker e **Aspire
  Dashboard standalone** (`mcr.microsoft.com/dotnet/aspire-dashboard`, UI `18888`, OTLP `18889`/`18890`
  no container, `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` — só local).
- Healthchecks em PostgreSQL e RabbitMQ; WebApi e Worker com `depends_on: condition: service_healthy`.
- `deploy/.env.example` com todas as variáveis; chaves de configuração `ConnectionStrings__*` e
  `OTEL_EXPORTER_OTLP_ENDPOINT` (as mesmas do AppHost).
- `Database__ApplyMigrationsOnStartup=true` no WebApi.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
apps/WebApi/Dockerfile
apps/Worker/Dockerfile
.dockerignore
deploy/**
```

## Critério de Aceite

- [ ] `docker compose up` sobe tudo sem passo manual
- [ ] Swagger, Aspire Dashboard e RabbitMQ Management acessíveis
- [ ] Uma compra aparece como um trace no painel
- [ ] Nenhum segredo real commitado

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `docker compose -f deploy/docker-compose.yml config -q`
— **Diretório:** raiz do repositório — **Esperado:** código de saída 0, sem saída

**Comando:** `docker compose -f deploy/docker-compose.yml up -d --wait`
— **Diretório:** raiz do repositório — **Esperado:** todos os serviços `healthy`/`running`; registrar nas notas as URLs verificadas


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
