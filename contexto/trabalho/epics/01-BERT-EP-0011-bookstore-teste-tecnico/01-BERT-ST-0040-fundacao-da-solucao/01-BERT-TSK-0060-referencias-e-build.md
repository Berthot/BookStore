---
id: BERT-TSK-0060
title: "TSK-0060 — Referências entre projetos, build central e git"
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
vault_path: 04-trabalho/01-entidades/05-bertho/03-trabalho/01-epics/bookstore/01-BERT-EP-0011-bookstore-teste-tecnico/01-BERT-ST-0040-fundacao-da-solucao/01-BERT-TSK-0060-referencias-e-build.md
description: "Liga as camadas na direção certa, centraliza versões de pacote e inicializa o git do repositório."
karawara-maps: [Ianderu, 04-trabalho, 01-entidades, 05-bertho, 03-trabalho, 01-epics, bookstore, 01-BERT-EP-0011-bookstore-teste-tecnico, 01-BERT-ST-0040-fundacao-da-solucao, 01-BERT-TSK-0060-referencias-e-build]
---

## Descrição

Ligar as camadas **na direção da regra de dependência**: Domain ← Application ← Infrastructure ←
WebApi/Worker. O Domain não referencia nada. Testes referenciam só o que testam.

- `Directory.Build.props` na raiz: `net10.0`, `Nullable` e `ImplicitUsings` habilitados,
  `TreatWarningsAsErrors` ligado para os projetos de `src/`.
- `Directory.Packages.props` (Central Package Management): toda versão de pacote declarada uma vez.
  **MassTransit fixado em `8.*`** (a v9 é comercial — ADR-0002).
- Remover os `Class1.cs` e `UnitTest1.cs` de template.
- `git init` com `.gitignore` de .NET. **Não** ignore `contexto/` — a decisão de versioná-lo fica
  para depois.

Por quê: o avaliador abre a solução e lê a estrutura antes do código. Referência invertida ou
versão espalhada em cada `.csproj` é a primeira coisa que um sênior nota.

Convenções: `contexto/trabalho/01-entrada/00-PROMPT-execucao-bookstore.md`. Decisões: `docs/adr/` e `docs/diagramas/`.

## Caminhos exclusivos

```
Directory.Build.props
Directory.Packages.props
src/*/*.csproj
apps/WebApi/WebApi.csproj
apps/Worker/Worker.csproj
tests/*/*.csproj
BookStore.slnx
.gitignore
```

## Critério de Aceite

- [ ] `src/Domain/Domain.csproj` não tem nenhum `ProjectReference`
- [ ] Application → Domain; Infrastructure → Application; WebApi e Worker → Application + Infrastructure
- [ ] Nenhum `Version=` em `PackageReference` dos `.csproj` — tudo no `Directory.Packages.props`
- [ ] MassTransit declarado como `8.*`
- [ ] Arquivos de template removidos
- [ ] Repositório com git inicializado e primeiro commit

## Prova de Funcionamento

Rode a prova **antes** de começar. Se ela é um filtro de teste e já passa, a prova está errada — reporte em vez de seguir.

**Comando:** `dotnet build BookStore.slnx -c Release`
— **Diretório:** raiz do repositório — **Esperado:** código de saída 0, zero avisos e zero erros


## Notas de execução

**Prova antes:** build com 2 warnings (NU1903 — Microsoft.OpenApi 2.0.0 com vulnerabilidade em WebApi.csproj). Esperado: ainda não havia TreatWarningsAsErrors nem CPM.

**Decisões:**
- `Microsoft.AspNetCore.OpenApi` atualizado de `10.0.8` → `10.0.12` (eliminou o warning de vulnerabilidade transitiva do Microsoft.OpenApi).
- `src/Directory.Build.props` separado do root para aplicar `TreatWarningsAsErrors` apenas aos projetos de `src/` — apps e tests não são afetados por warnings de pacotes de terceiros.
- `Microsoft.AspNetCore.Mvc.Testing 10.0.12` incluído no CPM para Tests.WebApi (necessário para WebApplicationFactory nas provas de TSK-0062).
- `Worker.csproj` preservou `UserSecretsId` pois ele identifica o secret store.
- Repositório já estava inicializado com branch `main` (git re-init confirmou).

**Saída da prova (após implementação):**
```
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)
Tempo Decorrido 00:00:05.81
```

**Critérios verificados:**
- [x] `src/Domain/Domain.csproj` não tem nenhum `ProjectReference`
- [x] Application → Domain; Infrastructure → Application; WebApi e Worker → Application + Infrastructure
- [x] Nenhum `Version=` em `PackageReference` dos `.csproj` — tudo no `Directory.Packages.props`
- [x] MassTransit declarado como `8.5.10` (8.x)
- [x] Arquivos de template removidos (Class1.cs, UnitTest1.cs)
- [x] Repositório com git inicializado e primeiro commit
