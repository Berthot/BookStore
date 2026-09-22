---
id: BERT-TSK-0067
title: "TSK-0067 — Regras transacionais: HighValueDigital, NewCustomerHighAmount, BulkQuantity, CardVelocity"
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
- '[[00-BERT-ST-0041-dominio-do-antifraude]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/02-BERT-ST-0041-dominio-do-antifraude/04-BERT-TSK-0067-regras-transacionais.md
description: "As quatro regras de fraude transacional, uma classe cada, com teste de disparo e de não disparo."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 02-BERT-ST-0041-dominio-do-antifraude, 04-BERT-TSK-0067-regras-transacionais]
---

## Descrição

Uma classe por regra implementando `IFraudRule`. Parâmetros como **constantes na classe**; quando
limite ou peso mudar, `Version` sobe (D-18). Cada motivo tem **código estável** (`HIGH_VALUE_DIGITAL`,
`NEW_CUSTOMER_HIGH_AMOUNT`, `BULK_QUANTITY`, `CARD_VELOCITY`) e texto placeholder. **Os testes
verificam o código, nunca o texto** (D-39).

Escolha limites e pesos que façam os cenários da demo acontecerem: S2 (e-book caro + cliente novo →
`Rejected`), S3 (10 unidades → `Review`), S5 (velocidade do cartão). Registre os valores nas notas.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
src/Domain/Rules/Transactional/**
tests/Tests.Domain/Rules/Transactional/**
```

## Critério de Aceite

- [ ] Cada regra tem teste que dispara e teste que não dispara
- [ ] Asserções sobre o código do motivo, nunca sobre o texto
- [ ] S2 somando as duas regras ultrapassa o limite de rejeição (teste com `FraudRuleSet` + `DecisionPolicy`)
- [ ] S3 cai em `Review` (teste)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test tests/Tests.Domain --filter "FullyQualifiedName~Rules.Transactional"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 8 testes


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
