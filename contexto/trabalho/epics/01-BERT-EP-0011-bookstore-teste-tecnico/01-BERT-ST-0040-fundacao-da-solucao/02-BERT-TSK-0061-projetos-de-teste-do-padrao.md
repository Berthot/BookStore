---
id: BERT-TSK-0061
title: "TSK-0061 — Tests.Shared, Tests.Domain e o padrão de testes"
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
- '[[00-BERT-ST-0040-fundacao-da-solucao]]'
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/01-BERT-ST-0040-fundacao-da-solucao/02-BERT-TSK-0061-projetos-de-teste-do-padrao.md
description: "Cria Tests.Shared e Tests.Domain e padroniza os projetos de teste (NUnit, AwesomeAssertions, NSubstitute, categorias)."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 01-BERT-ST-0040-fundacao-da-solucao, 02-BERT-TSK-0061-projetos-de-teste-do-padrao]
---

## Descrição

Segue o padrão de testes de Bertho (resumido no prompt de execução):

- `tests/Tests.Shared`: `Attributes/TestCategories.cs` (`[Unit]`, `[Integration]`, `[Slow]`,
  `[Critical]`), `Base/UnitTestsBase.cs`, `Constants/TestConstants.cs`, `Mothers/<Contexto>/`
  (vazio por enquanto — as Mothers nascem com as entidades).
- `tests/Tests.Domain`: referencia só `Domain` e `Tests.Shared`.
- Todos os projetos de teste: NUnit, **AwesomeAssertions** (nunca FluentAssertions — licença),
  NSubstitute. `Tests.Shared` não depende de banco nem de infraestrutura.
- Um teste de sanidade `[Unit]` em `Tests.Domain` para a prova ter o que rodar; ele será
  substituído pelos testes reais.

Por quê: o domínio é o código mais testável do projeto e não tinha projeto de teste (D-44).

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
tests/Tests.Shared/**
tests/Tests.Domain/**
tests/*/*.csproj
BookStore.slnx
Directory.Packages.props
```

## Critério de Aceite

- [ ] `Tests.Shared` e `Tests.Domain` existem e estão no `BookStore.slnx`
- [ ] Os quatro atributos de categoria existem e herdam de `CategoryAttribute`
- [ ] Nenhum projeto de teste referencia FluentAssertions
- [ ] `Tests.Shared` não referencia Infrastructure nem pacotes de banco
- [ ] `.slnx` relido imediatamente antes de ser editado

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test BookStore.slnx --filter "TestCategory=Unit"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 1 teste


## Notas de execução

> Preenchido pelo executor: o que foi feito, decisões tomadas, saída da prova, commit.
