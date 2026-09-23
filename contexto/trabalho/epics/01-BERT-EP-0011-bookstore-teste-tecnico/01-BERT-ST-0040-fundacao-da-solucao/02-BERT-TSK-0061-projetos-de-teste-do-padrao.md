---
id: BERT-TSK-0061
title: "TSK-0061 — Tests.Shared, Tests.Domain e o padrão de testes"
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

- [x] `Tests.Shared` e `Tests.Domain` existem e estão no `BookStore.slnx`
- [x] Os quatro atributos de categoria existem e herdam de `CategoryAttribute`
- [x] Nenhum projeto de teste referencia FluentAssertions
- [x] `Tests.Shared` não referencia Infrastructure nem pacotes de banco
- [x] `.slnx` relido imediatamente antes de ser editado

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet test BookStore.slnx --filter "TestCategory=Unit"`
— **Diretório:** raiz do repositório — **Esperado:** todos aprovados, pelo menos 1 teste


## Notas de execução

**Prova antes:** 0 testes encontrados (filtro `TestCategory=Unit` sem testes). Esperado — projeto Tests.Shared não existia.

**Decisões:**
- `Tests.Shared.csproj` criado com AwesomeAssertions + NSubstitute; sem referência a Infrastructure ou banco.
- `Attributes/TestCategories.cs`: atributos `[Unit]`, `[Integration]`, `[Slow]`, `[Critical]` herdam `CategoryAttribute` via primary constructor.
- `Base/UnitTestsBase.cs`: classe abstrata decorada com `[Unit]` — classes que herdam recebem a categoria automaticamente.
- `Constants/TestConstants.cs`: IDs e datas fixas reutilizáveis por Mothers/Builders.
- Pastas `Mothers/Catalog`, `Mothers/Sales`, `Mothers/FraudAnalysis` criadas vazias via `<Folder>` no `.csproj` — serão preenchidas junto das entidades.
- `Tests.Domain` atualizado para referenciar `Tests.Shared`.
- Teste de sanidade `SanityTests.cs` em `Tests.Domain` com `[Unit]`.
- `Tests.Shared` adicionado ao `BookStore.slnx` na pasta `/tests/`.

**Saída da prova (após implementação):**
```
Aprovado!  – Com falha: 0, Aprovado: 1, Ignorado: 0, Total: 1, Duração: 33 ms
```

**Critérios verificados:**
- [x] `Tests.Shared` e `Tests.Domain` existem e estão no `BookStore.slnx`
- [x] Os quatro atributos de categoria existem e herdam de `CategoryAttribute`
- [x] Nenhum projeto de teste referencia FluentAssertions
- [x] `Tests.Shared` não referencia Infrastructure nem pacotes de banco
- [x] `.slnx` relido imediatamente antes de ser editado
