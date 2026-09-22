---
id: BERT-TSK-0077
title: "TSK-0077 — Métricas de negócio e spans das regras"
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
- '[[00-BERT-ST-0043-antifraude-api-e-worker]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/04-BERT-ST-0043-antifraude-api-e-worker/05-BERT-TSK-0077-metricas-de-negocio.md
description: "As seis métricas de negócio do ADR-0005 e spans próprios na avaliação."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 04-BERT-ST-0043-antifraude-api-e-worker, 05-BERT-TSK-0077-metricas-de-negocio]
---

## Descrição

`System.Diagnostics.Metrics` com um `Meter` por serviço:

| Métrica | Tipo |
| :--- | :--- |
| `fraud.transactions.received` | contador |
| `fraud.decisions` (tag `outcome`) | contador |
| `fraud.rule.hits` (tag `rule_code`) | contador |
| `fraud.decision.duration` | histograma (recebimento → decisão) |
| `bookstore.purchases` (tag `status`) | contador |
| `idempotency.replays` | contador |

`ActivitySource` próprio com spans em: avaliação das regras, decisão da política, gravação da decisão.
Registrados em `TelemetryExtensions`.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

Seguir a **§8 Padrões .NET** do prompt: `<CasoDeUso>Request` implementa `ICommand<OperationResult<...Response>>` (escrita) ou `IQuery<...>` (leitura); handler `sealed` devolve `OperationResult<T>` (nunca exceção para fluxo esperado); validator FluentValidation na mesma pasta; repositórios nunca chamam `SaveChanges` — **um `CommitAsync` do Unit of Work** no fim do caminho de sucesso; `CancellationToken` último parâmetro e propagado.

## Caminhos exclusivos

```
src/Application/Diagnostics/**
src/Infrastructure/Extensions/TelemetryExtensions.cs
tests/Tests.Application/Diagnostics/**
```

## Critério de Aceite

- [ ] As seis métricas existem com os nomes da tabela
- [ ] `fraud.rule.hits` incrementa uma vez por regra disparada (teste com `MeterListener`)
- [ ] Spans aparecem com nome de negócio

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Application --filter "FullyQualifiedName~Diagnostics"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 2 testes


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
