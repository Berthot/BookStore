---
id: BERT-TSK-0086
title: "TSK-0086 — Preencher o How to use e os Testes do README"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/06-BERT-ST-0045-execucao-e-demo/04-BERT-TSK-0086-readme-how-to-use.md
description: "Substitui os placeholders do README pelas instruções reais verificadas."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 06-BERT-ST-0045-execucao-e-demo, 04-BERT-TSK-0086-readme-how-to-use]
---

## Descrição

Preencher **só** as seções marcadas com ⏳ em `README.md`: pré-requisitos, comando, URLs reais,
Postman e a coluna "Como executar" dos cenários; e a seção 🧪 Testes com os comandos reais.

**Não** preencher "🤖 Uso de IA no processo" — é do Bertho. Não alterar as outras seções nem
`docs/` (a documentação já é a fonte da verdade). Cada comando escrito no README foi **executado** antes.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
README.md
```

## Critério de Aceite

- [ ] Nenhum ⏳ restante exceto em Uso de IA
- [ ] Todo comando do README foi executado e está nas notas
- [ ] Nenhuma outra seção alterada (diff restrito)

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `git diff --stat -- README.md`
— **Diretório:** raiz do repositório — **Esperado:** apenas `README.md` alterado


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
