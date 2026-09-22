# Prompt de execução — BookStore (teste técnico .NET)

> Para o agente executor (Claude Code) neste repositório. Leia inteiro antes da primeira task.
>
> ⚠️ **Atualizado em 2026-09-22:** entrou a **§8 Padrões .NET** (Cortex.Mediator, OperationResult,
> FluentValidation, Repository/Unit of Work, Options, exceções, workers, seed). Se você já passou da
> TSK-0061, releia a §8 antes de continuar e ajuste o que conflitar.

## 1. O que é este projeto

Teste técnico para uma vaga sênior .NET: um **módulo antifraude** (decide `APPROVED`, `REJECTED`,
`REVIEW`) consumido por um **marketplace de livros**. A empresa vai **clonar e executar** o projeto e
depois Bertho vai **defender cada decisão** numa banca. O tema que os devs da empresa citaram é
**idempotência** — trate-o como o ponto mais sensível do código.

## 2. Fonte da verdade

- `README.md` e `docs/` (ADRs, diagramas, contrato de API) **já estão escritos e são a fonte da
  verdade**. O código implementa o que eles afirmam.
- Se o código precisar divergir da documentação, **pare e reporte nas notas da task** — não altere
  `docs/`, os ADRs nem o README (exceto a TSK-0086, que preenche só os placeholders ⏳).
- O trabalho está em `contexto/trabalho/epics/01-BERT-EP-0011-bookstore-teste-tecnico/`: 1 épico,
  7 stories, 30 tasks. As decisões `D-xx` citadas nas tasks estão no Apêndice A.

## 3. Ordem

Stories em sequência: **ST-0040 → ST-0041 → ST-0042 → ST-0043 → ST-0044 → ST-0045 → ST-0046**. Dentro de
cada story, a ordem está no arquivo da story. **ST-0046 (testes integrados) só depois de tudo o mais
concluído** — é extra.

Por quê: cada camada assume a anterior pronta; o domínio vem antes da persistência para que as
regras nasçam sem banco.

## 4. Protocolo por task

1. Abrir a task e marcar `status: em-andamento` no frontmatter.
2. **Rodar a prova antes de começar.** Se for um filtro de teste e já passar, a prova está errada —
   reporte em vez de seguir.
3. Implementar tocando **só** os `Caminhos exclusivos`. Precisou tocar outro arquivo? Registre nas notas
   e o motivo.
4. Rodar a prova. Verde → colar a saída relevante em `## Notas de execução`, marcar os checkboxes do
   Critério de Aceite que foram de fato verificados.
5. Marcar `status: concluido` (**sem acento** — `concluído` é inválido).
6. Commit **por task**, adicionando arquivos pelo nome (**nunca `git add -A`**):
   `feat(<contexto>): <assunto> [BERT-TSK-00xx]`. **Nunca `git push`** — push é do Bertho.

Os arquivos em `contexto/` voltam ao vault por sincronização — mantenha frontmatter e seções intactos.

## 5. Convenções de código

> Resumo. O detalhe de CQRS, resultado, validação, persistência e configuração está na §8.

- **Camadas:** Domain ← Application ← Infrastructure ← WebApi/Worker. Domain não referencia nada.
- **Casos de uso:** `src/Application/UseCases/<Contexto>/<CasoDeUso>/[<Nome>Request.cs,
  <Nome>Response.cs, <Nome>Handler.cs]`. Contextos: `Catalog`, `Sales`, `FraudAnalysis`. Entidades
  espelham: `src/Domain/Entities/<Contexto>/`.
- **DI segmentada**, extensions por tecnologia em `src/Infrastructure/Extensions/`.
  **`<summary>` sempre em inglês.** Valores de política (retry, limites, janelas) em constantes nomeadas
  com o porquê no `<summary>`.
- **Monólito modular:** WebApi só HTTP; Worker hospeda todos os consumidores e jobs. Sales só fala com
  FraudAnalysis pela porta `IFraudCheckGateway` e por mensagens.
- **API:** `/api/v1` via `MapGroup`; enums `SNAKE_UPPER`; Problem Details; `X-Correlation-Id` só no
  header; formato exato de `docs/api/contrato.md`.
- **MassTransit fixado em `8.*`** (v9 é comercial).
- **Regras antifraude puras:** sem banco, sem rede; sinais calculados pelo caso de uso.

## 6. Padrão de testes

- NUnit + **AwesomeAssertions** (nunca FluentAssertions) + NSubstitute.
- `tests/Tests.Shared`: `Attributes/TestCategories.cs` (`[Unit]`, `[Integration]`, `[Slow]`,
  `[Critical]`), `Mothers/<Contexto>/` com Mother + Builder espelhando o domínio, `Constants/`,
  `Base/UnitTestsBase.cs`.
- **Proibido** instanciar entidade no teste sem Mother/Builder. A Mother devolve instância válida; o teste
  altera só o campo do cenário.
- AAA com comentários em testes não triviais; `_sut` para o sistema sob teste; `[TestCase]` para
  variações; nomes `Metodo_Cenario_Resultado`; um assert por teste sempre que possível.
- Pasta do teste **espelha** a pasta do código.
- Unitário em tudo; integrado **só** nos 4 casos da ST-0046, com Testcontainers — **nunca EF InMemory**.
- Asserções sobre **código** do motivo das regras, nunca sobre o texto.

## 8. Padrões .NET (obrigatório a partir da TSK-0062)

> Acrescentado após a abertura da rodada. Vem do padrão de implementação de Bertho, conciliado com as
> decisões deste projeto. Onde houver conflito, **esta seção vence** o que você traria de hábito.

### 8.1 CQRS com Cortex.Mediator

- **Cortex.Mediator** (MIT), versão estável mais recente **3.x**. **Nunca MediatR** (licença comercial).
- API atual: `ICommand<TResult>` / `ICommandHandler<TCommand, TResult>` para escrita;
  `IQuery<TResult>` / `IQueryHandler<TQuery, TResult>` para leitura. **Não use `IRequest<>`** — exemplos
  antigos com `IRequest` estão desatualizados. Confirme nomes de interface e a assinatura de
  `AddCortexMediator` compilando contra o pacote instalado; se divergirem do descrito aqui, registre nas notas.
- **Nomenclatura (D-11 vence o padrão antigo `{Ação}{Entidade}Command`):**
  `UseCases/<Contexto>/<CasoDeUso>/`
  - `<CasoDeUso>Request.cs` — `sealed record` que implementa `ICommand<OperationResult<<CasoDeUso>Response>>`
    (escrita) ou `IQuery<OperationResult<<CasoDeUso>Response>>` (leitura). **É a interface que diz se é
    comando ou consulta.**
  - `<CasoDeUso>Response.cs` — saída; DTOs aninhados com sufixo `Dto`.
  - `<CasoDeUso>Handler.cs` — `sealed`, primary constructor, injeta **ports** (interfaces), nunca adapters.
  - `<CasoDeUso>RequestValidator.cs` — quando houver validação de entrada.
- Registro do mediator **só** em `Application/DependencyInjection.cs` (Program não chama `AddCortexMediator`).
- Endpoints (Minimal API, `MapGroup("/api/v1")`) são finos: montam o `Request`, despacham e convertem o
  resultado. Nenhuma regra no endpoint.

### 8.2 OperationResult

- `Application/Commons/ErrorCode.cs`: `None`, `Validation`, `NotFound`, `Conflict`, `Unprocessable`,
  `Unauthorized`, `Forbidden`, `Internal`.
- `Application/Commons/OperationResult.cs`: base **não genérica** `OperationResult` (`IsSuccess`,
  `ErrorCode`, `Errors`) + `OperationResult<T>` (`Data`), propriedades `init`, fábricas `SuccessResult(data)`,
  `Fail(code, error)`, `Fail(code, errors)`.
- **Todo handler devolve `OperationResult<T>`.** Fluxo esperado (não encontrado, conflito, validação) é
  resultado, **nunca exceção**.
- Extensão `ToHttpResult()` na WebApi: `Validation → 400`, `NotFound → 404`, `Conflict → 409`,
  `Unprocessable → 422`, `Unauthorized → 401`, `Forbidden → 403`, `Internal → 500`, sempre Problem Details.
  Sucesso com status explícito (`202` nos POST de criação, `200` nas consultas e na revisão).
- **Distinção 400 × 422:** entrada malformada (validator) → `400`; violação de regra de domínio com entrada
  bem formada → `422` (ex.: I-05, justificativa ausente na revisão). Estado incompatível → `409` (ex.: I-04).

### 8.3 Validação

- **FluentValidation**, validator na **mesma pasta** do caso de uso (nada de `Application/Validators/`).
- Registro por assembly scan (`AddValidatorsFromAssembly`).
- `Application/Behaviors/ValidationBehavior` como pipeline behavior do Cortex para comandos **e**
  consultas: roda os validators e, em falha, **devolve** `OperationResult.Fail(ErrorCode.Validation, erros)`
  — **não lança exceção**. Constraint `where TResult : OperationResult`.
- Behaviors: telemetria (externo) → guarda de exceção (converte exceção inesperada em `Internal`, relança
  `OperationCanceledException`) → validação (interno). Ordem de registro = ordem de execução.

### 8.4 Domínio

- Classe base `Entity` abstrata com `Guid Id { get; init; }`. Entidades `sealed` quando não são base.
- Comportamento por métodos (`StartProcessing()`, `Decide(...)`); nada de setter público para estado.
- Auditoria de tempo: `CreatedAt` (obrigatório, nunca muda) e `UpdatedAt` (opcional), em **UTC**;
  PostgreSQL `timestamptz`. `UpdatedAt` só quando algo muda de fato.

### 8.5 Repositório e Unit of Work

- `IRepository<TEntity> where TEntity : Entity` + repositórios específicos quando preciso
  (`ITransactionRepository`, `IPurchaseRepository`…), métodos `...Async` com `CancellationToken`.
- **Repositórios nunca chamam `SaveChangesAsync`** — só anexam ao contexto.
- `IUnitOfWork` com um único `Task<bool> CommitAsync(CancellationToken)`. Dois bancos lógicos →
  interfaces derivadas: `IBookStoreUnitOfWork` e `IFraudUnitOfWork` (evita conflito de DI).
- O **handler** orquestra e chama `CommitAsync` **na última linha de sucesso**. Retornou `Fail` antes?
  Nada é gravado. É aqui que a chave de idempotência, a entidade e a mensagem da outbox entram juntas:
  **um `CommitAsync` por caso de uso** (publicar no `IPublishEndpoint` do escopo **antes** do commit, para a
  outbox capturar).
- Registro explícito de cada repositório (`AddScoped<IX, X>()`), não por scan.

### 8.6 Configuração

- Classes de configuração com sufixo `Options`, validadas com DataAnnotations +
  `.ValidateDataAnnotations().ValidateOnStart()` (a app não sobe com config inválida).
- Consumidores injetam `IOptions<T>`; **`IConfiguration` só em `DependencyInjection.cs` e `Program.cs`**.
- Exceção deliberada: **parâmetros das regras antifraude são constantes na classe da regra**, não
  `Options` (D-18 — a versão da regra não pode descolar do comportamento).

### 8.7 Erros não tratados

- `GlobalExceptionHandler : IExceptionHandler`, `sealed`, primary constructor (`ILogger`,
  `IWebHostEnvironment`). Loga a exceção completa; `Detail` com stack trace só em Development, texto genérico
  fora dele; `traceId` em `ProblemDetails.Extensions`; `Status 500`, `Title "Internal"`.

### 8.8 Assincronia e cancelamento

- `CancellationToken` sempre o **último** parâmetro e **sempre propagado** (repositório, DbContext, HTTP).
- Sufixo `Async` em tudo que retorna `Task` (exceto `Handle`). Nunca `async void`, `.Result` ou `.Wait()`.
- Consultas independentes (ex.: os sinais do `FraudContext`) podem rodar com `Task.WhenAll` **se** usarem
  contextos distintos — um mesmo `DbContext` não aceita consultas concorrentes.

### 8.9 Workers e jobs

- Consumidores são classes do MassTransit (sem `BackgroundService` manual).
- Jobs periódicos (`BackgroundService` + `PeriodicTimer`): injetam `IServiceScopeFactory`, criam um
  escopo **por iteração**, `try/catch` envolvendo a iteração inteira (uma exceção não pode derrubar o host),
  propagam o `stoppingToken`. A lógica mora num caso de uso testável; o job só dispara.

### 8.10 Seed

- EF Core 9+ `UseAsyncSeeding` com um seeder por contexto (`CatalogDataSeeder`), idempotente (verifica
  antes de inserir), ligado por `SeedingOptions.Enabled`.

## 7. Discorde

Se uma premissa deste documento ou de uma task estiver errada, **diga nas notas** antes de contornar.
Uma premissa furada reportada vale mais que uma task ruim cumprida.

## Apêndice B — Convenções de persistência e mensageria

Geração de migrations — sempre pela CLI, uma por contexto:

```bash
dotnet ef migrations add <Nome> --context FraudDbContext \
  --project src/Infrastructure --startup-project apps/WebApi \
  --output-dir Persistence/Fraud/Migrations

dotnet ef migrations add <Nome> --context BookStoreDbContext \
  --project src/Infrastructure --startup-project apps/WebApi \
  --output-dir Persistence/BookStore/Migrations
```

- Proibido criar ou editar arquivo de migration à mão (exceto revisão do gerado).
- Cada contexto: `HasDefaultSchema(...)` + `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")`.
- `ApplyConfigurationsFromAssembly(asm, t => t.Namespace?.StartsWith("<Root>.Persistence.Fraud") == true)` (idem `Persistence.BookStore`) — sem filtro, um contexto mapeia as entidades do outro.
- Layout: `src/Infrastructure/Persistence/{BookStore,Fraud}/{<X>DbContext.cs, <X>DbContextFactory.cs, Configurations/<Contexto>/, Migrations/}`.
- Tabelas do MassTransit (`InboxState`, `OutboxMessage`, `OutboxState`) mapeadas em cada DbContext que publica/consome (`AddInboxStateEntity`, `AddOutboxMessageEntity`, `AddOutboxStateEntity`) e criadas pelas migrations normais — nada à mão.
- MassTransit fixado em `8.*` (v9 é comercial).
- Toda política de resiliência (intervalos de retry, limites de reconciliação, filas) em extension dedicada (`MassTransitExtensions`, `ReconciliationExtensions`…), com valores em constantes nomeadas e `<summary>` em inglês explicando o porquê de cada valor.
- Aplicar com `await db.Database.MigrateAsync(ct)` direto — **nunca** dentro de transação explícita (no EF Core 9+ isso impede o lock contra migrações concorrentes).
- Repositório: `D:\\Projetos\\Externos\\BookStore` (sem git ainda). `BookStore.slnx`, .NET 10, NUnit. Projetos: `src/{Domain,Application,Infrastructure}`, `apps/{WebApi,Worker}`, `tests/Tests.{Application,Infrastructure,WebApi,Worker}`. Nos comandos: `<Infra>` = `src/Infrastructure`, `<Host>` = `apps/WebApi`.


## Apêndice A — Decisões (D-xx citadas nas tasks)

| # | Decisão |
| :--- | :--- |
| D-01 | Âncora governada por Bertho, escopo isolado |
| D-03 | Agregado: `Transaction` contém seus `Assessments` |
| D-04 | Cenário: livraria (book store), projeto novo do zero |
| D-05 | `CorrelationId` para tracking, distinto de `Idempotency-Key` e `TransactionId` |
| D-06 | Status (etapa) e Outcome (decisão) são campos separados |
| D-07 | Auditoria nunca sobrescreve: revisão humana gera novo `Assessment` |
| D-08 | Ordem: documentação → código do antifraude (com testes unitários) → marketplace → testes integrados (extra, por último) |
| D-09 | Antifraude expõe o contrato literal (`POST`/`GET /api/v1/transactions`); "comprar livro" usa o mesmo caso de uso `SubmitTransaction` via porta (ver D-26) |
| D-10 | Rotas sob `/api/v1/` (versionamento por URL) |
| D-11 | `UseCases/<Contexto>/<CasoDeUso>/[Request, Response, Handler]` — Catalog, Sales, FraudAnalysis |
| D-12 | Documentar no módulo bookstore do vault; exportar depois ao repositório |
| D-13 | Sem entidade `ManualReview`: revisão humana = `Assessment` com `DecidedBy = Reviewer` + justificativa |
| D-14 | Contextos só se referenciam por id (`Purchase` → `BookId`, `TransactionId`) |
| D-15 | Regras: `IFraudRule` + uma classe por regra; `FraudRuleSet` compõe; `DecisionPolicy` converte score em Outcome |
| D-16 | Regras são puras sobre `FraudContext`; o caso de uso pré-calcula sinais que exigem banco |
| D-17 | Antifraude não conhece livros: livraria envia `Delivery` (Digital/Physical) e `ItemCount` |
| D-18 | Parâmetros das regras são constantes na classe; externalizar (versão = hash dos parâmetros) fica como evolução |
| D-19 | Mensageria: RabbitMQ (ADR-0002) |
| D-20 | Banco: PostgreSQL com 2 schemas (`bookstore`, `fraud`) (ADR-0003) |
| D-21 | Idempotência só no PostgreSQL (índice único na `Idempotency-Key`); Redis descartado — registrar no ADR-0004 |
| D-22 | EF Core: um `IEntityTypeConfiguration<T>` por entidade, separado das entidades; migrations organizadas |
| D-23 | Dois DbContexts (`BookStoreDbContext`, `FraudDbContext`): schema padrão próprio, pasta de migrations própria, `__EFMigrationsHistory` no próprio schema; configurations aplicadas com filtro por namespace |
| D-24 | Migrations geradas só via `dotnet ef migrations add` (nunca escritas à mão); um `IDesignTimeDbContextFactory` por contexto |
| D-25 | Migrations aplicadas no startup do `WebApi` (`MigrateAsync`, só as pendentes, os dois contextos), ligado por flag de configuração; `Worker` não migra. Produção: etapa dedicada do pipeline (registrar no README) |
| D-26 | Monólito modular: um `WebApi` + um `Worker` compartilhando `src/`. Sales depende de uma porta própria (`IFraudCheckGateway`); adapter chama `SubmitTransaction` em processo — trocar por cliente HTTP se o antifraude virar serviço |
| D-27 | Sales sabe da decisão pelo evento `transaction.decided` (RabbitMQ, via outbox). O `GET /api/v1/transactions/{id}` continua existindo — é requisito e é a forma do avaliador verificar |
| D-28 | Execução: `docker compose up` (só Docker) é o caminho do avaliador e o artefato de deploy (ADR-0007), com Aspire Dashboard standalone (`localhost:18888`, anônimo só local). Aspire AppHost é a orquestração de desenvolvimento. Os dois usam as mesmas chaves de configuração (`ConnectionStrings__*`, `OTEL_EXPORTER_OTLP_ENDPOINT`) |
| D-29 | Pastas: `apps/AppHost` (projeto na solution) e `deploy/` (`docker-compose.yml`, `.env.example`) |
| D-30 | Mensageria via MassTransit **v8** (Apache 2.0) sobre RabbitMQ: Bus Outbox + Consumer Inbox no EF Core, retry com backoff, fila `_error` como DLQ. Não é CDC. ADR registra licença (v9 comercial; v8 com patches até fim de 2026) e alternativas (outbox própria, Debezium, Wolverine, Rebus) |
| D-31 | WebApi só HTTP; Worker hospeda todos os consumidores (inclusive o do Sales, `transaction-decided`). Os dois compartilham `src/` |
| D-32 | `Domain/Entities/<Contexto>/` com as mesmas subpastas de contexto dos `UseCases` (Catalog, Sales, FraudAnalysis) |
| D-33 | Infrastructure por `Persistence/<DbContext>/` (DbContext, factory design-time, `Configurations/<Contexto>/`, `Migrations/`). Substitui as pastas vazias `Configurations/` e `Data/` |
| D-34 | `POST /transactions` segue o draft IETF `draft-ietf-httpapi-idempotency-key-header-07` e o Stripe: sem chave → `400`; chave nova → `202` + `Location`; repetida com mesmo payload → replay da resposta original; payload diferente → `422`; original ainda em processamento → `409`. Guarda SHA-256 do corpo normalizado junto da chave, na mesma transação |
| D-35 | `DependencyInjection.cs` de Application e Infrastructure segmentados por responsabilidade; XML `<summary>` sempre em inglês. Postgres, RabbitMQ, MassTransit, Telemetry etc. expostos como extension methods reutilizáveis em arquivos próprios em `src/Infrastructure/Extensions/` (`PostgresExtensions`, `MassTransitExtensions` — RabbitMQ como transporte —, `TelemetryExtensions`); `AddPostgresDbContext<TContext>(config, schema)` genérico serve aos dois contextos. WebApi e Worker compõem só o que usam |
| D-36 | Compra assíncrona via outbox: `PurchaseBook` grava `Purchase` + `purchase-placed` na outbox do schema `bookstore` (uma transação) e responde `202`; consumidor no Worker chama `SubmitTransaction` pela porta com `Idempotency-Key = PurchaseId`. Cada transação de banco toca um schema só |
| D-37 | Fallback em 3 camadas: (1) retry com backoff do MassTransit; (2) DLQ `_error`; (3) job de reconciliação no Worker que republica `purchase-placed` para `Purchase` em `PendingFraudCheck` além de um limite — seguro por idempotência (chave = PurchaseId, inbox, checagem de estado). Cada política vive numa extension dedicada e documentada |
| D-38 | Ciclo da `Purchase`: `PendingFraudCheck` → `Confirmed` (só com `Approved`) / `Cancelled` (`Rejected`) / `UnderReview` (`Review`, inclusive fail-safe quando a avaliação esgota tentativas). Adotado sem objeção |
| D-39 | Toda regra registra um motivo (texto placeholder por enquanto) no `RuleEvaluation`; cenários de reprovação cobertos por testes unitários |
| D-40 | Por ser desafio técnico, as respostas mostram os dois públicos em campos separados: mensagem genérica ao comprador + detalhes do antifraude (regras, códigos, motivos). README registra que em produção os detalhes ficariam atrás de autorização |
| D-41 | `POST /api/v1/purchases` também exige `Idempotency-Key` com o mesmo comportamento da D-34; a idempotência é um componente compartilhado, não código duplicado. Diagramas 01, 02 e 03 prontos |
| D-42 | Narrativa: **marketplace de livros** (Catalog + Sales) que **consome** o antifraude. Separação lógica, por contexto — não por processo. D-31 mantida |
| D-43 | Idempotência: filtro HTTP (header obrigatório → `400`, SHA-256, replay/`422`/`409`) + `IIdempotencyStore` na aplicação + tabela `idempotency_keys` **em cada schema**, gravada no mesmo `SaveChanges` do caso de uso e da outbox. Mecanismo compartilhado; dados nunca. Dois serviços lógicos (marketplace, antifraude), cada um dono do seu schema |
| D-44 | Criar `tests/Tests.Domain` e `tests/Tests.Shared`. Seguir o padrão de testes do módulo dotnet do Bertho (13-testing, 28-testing-setup, 29-testing-fundamentals): NUnit + AwesomeAssertions + NSubstitute; Object Mother + Builder espelhando os agregados em `Tests.Shared/Mothers/<Contexto>/`; categorias `[Unit]`/`[Integration]`/`[Slow]`/`[Critical]`; AAA; `_sut`; `[TestCase]` |
| D-45 | Unitário primeiro; integrado só pontual. `Tests.Domain`, `Tests.Application`, `Tests.WebApi`, `Tests.Worker` = `[Unit]` (sem banco, NSubstitute). `Tests.Infrastructure` = `[Integration]` com Testcontainers PostgreSQL real (desvio consciente do EF InMemory do padrão: sem transação, não relacional — doc oficial desaconselha). Filtro: `dotnet test --filter TestCategory=Unit` |
| D-46 | Escopo integrado fechado em 4: (1) idempotência pelo índice único; (2) atomicidade entidade+chave+outbox; (3) migrations dos 2 contextos nos schemas certos; (4) concorrência de requisições iguais → uma aceita, outra `409`/replay. Regra: só é integrado se um mock mentiria |
| D-47 | Testes espelham as pastas do código (`Tests.Domain/Entities/<Contexto>/`, `Rules/`, `Tests.Application/UseCases/<Contexto>/<CasoDeUso>/`…); nomes `Metodo_Cenario_Resultado`; `PostgresContainerFixture` sobe **um** container por execução. README com roteiro (`--filter TestCategory=Unit` sem Docker; `dotnet test` completo). **Testes integrados são o último extra** — depois de todo o resto |
| D-48 | Estado `Failed` removido. `Transaction`: `Received → Processing → Decided` (retry reentra em `Processing`; fail-safe `FailSafe()` grava `Review`/System). Nova invariante I-07. Diagrama 04 pronto — os 4 diagramas estão feitos |
| D-49 | Endpoint extra `POST /api/v1/transactions/{id}/review` (`outcome` Approved/Rejected, `reviewerId` opcional, `justification` obrigatória). Aplica I-04/I-05 (`409`/`422`), cria novo `Assessment` e publica `transaction-decided` via outbox — reaproveita o fluxo existente. Sem autenticação: `reviewerId` é placeholder, com comentário no código explicando a decisão |
| D-50 | Premissa: existe **um único revisor**. O endpoint aceita qualquer `reviewerId` (ou vazio) e o `Assessment` grava/retorna um id genérico constante (`DefaultReviewerId`). Comentário no código registra a premissa e que em produção o id viria do token |
| D-51 | `RuleEvaluation` em tabela própria (não `jsonb`) para auditoria por regra; índice `(payment_fingerprint, occurred_at)` para regras de velocidade. Sem `jsonb`: tabelas e relações |
| D-52 | As regras concretas são definidas **a partir dos cenários de simulação** (cada cenário da demo precisa de uma regra que o produza) |
| D-53 | Cenários de simulação S1–S11 (cada um = chamada no `.http` + passo no README + teste): S1 aprovado · S2 e-book caro/cliente novo → Rejected · S3 10 unidades → Review · S4 revisor aprova S3 → Confirmed · S5 velocidade do cartão · S6 replay · S7 `422` · S8 `400` · S9 parar o Worker e religar (resiliência ao vivo) · S10 desvio de valor → Review · S11 fracionamento → Review |
| D-54 | Regras de discrepância (`AmountDeviationRule`, `StructuringRule`) pesam para `Review` (sinal, não prova). Modelo e catálogo de regras documentados no **ADR-0008** (novo). Conluio vendedor-comprador = evolução documentada |
| D-55 | ADR-0004: idempotência em 5 pontos; concorrência via `lock_timeout` curto (replay se a 1ª confirmou, `409` se esgotou); validação antes de registrar a chave; chave até 255 chars (UUID v4); falha de execução faz rollback da chave (diferente do Stripe). Retenção de 24h com job de limpeza no Worker (confirmado) |
| D-56 | ADR-0005: OpenTelemetry (traces/métricas/logs) via OTLP em `TelemetryExtensions`; 6 métricas de negócio (objetivo: demonstrar implementação e gestão de métricas) |
| D-57 | ADR-0006 (versão na URL, `MapGroup("/api/v1")`, política de evolução) e ADR-0007 (entregue: Dockerfile multi-stage por app + compose com healthchecks + `.env.example` + seed; produção descrita: Kubernetes, HPA na API, KEDA no Worker, migrations como etapa do pipeline). **Todos os 8 ADRs prontos** |
| D-58 | Contrato de API escrito: erros em Problem Details (RFC 9457); dinheiro `{value, currency}`; datas ISO 8601 UTC; enums como texto; `X-Correlation-Id` sempre devolvido; `GET /transactions/{id}` com `decision` + `history`; `customerMessage` igual para `PendingFraudCheck` e `UnderReview` (não revelar revisão). Review sem `Idempotency-Key` — idempotente pela regra de estado (`409`) — adotado sem objeção |
| D-59 | `CorrelationId` e `traceId` mantidos separados: trace = uma execução; CorrelationId = fluxo de negócio (sobrevive a reconciliação, revisão e amostragem). Tabela da diferença no ADR-0005 |
| D-60 | Na API, correlação só pelo header `X-Correlation-Id` (sai do corpo JSON); no código, uma propriedade `CorrelationId`, persistida no banco e nas mensagens |
| D-61 | Apresentação e testes dos avaliadores via Swagger/Postman |
| D-62 | Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI` sobre o documento nativo do `Microsoft.AspNetCore.OpenApi`), com `Idempotency-Key` declarado obrigatório e exemplos válidos por endpoint. Collection do Postman em `docs/postman/` com os 11 cenários em ordem, script gerando `Idempotency-Key` e request que repete a chave (S6) |
| D-63 | README criado (emojis, sumário, tabela "desafio → onde está", How to use com placeholders ⏳, seções exigidas resumidas com diagramas embutidos e links). Diagramas embutidos marcados `<!-- sincronizado de ... -->` |
| D-64 | Todos os enums na API em maiúsculas com underscore (`JsonStringEnumConverter` + `JsonNamingPolicy.SnakeCaseUpper`): decisão literal ao enunciado (`APPROVED`/`REJECTED`/`REVIEW`) e demais enums consistentes (`PENDING_FRAUD_CHECK`, `DIGITAL`…). No código, enums em PascalCase |
| D-65 | "Serviço externo de risco" como ponto de extensão documentado no diagrama de componentes (tracejado, não implementado). Consulta externa acontece no caso de uso ao montar o `FraudContext` (timeout + circuit breaker), **nunca dentro da regra**; indisponível = sinal "indisponível". Diagrama corrigido: `purchase-placed` e reconciliação agora aparecem |
| D-36 | "Comprar livro" síncrono (opção A): grava `Purchase` (bookstore) e chama `SubmitTransaction` via porta com `Idempotency-Key = PurchaseId`. Idempotência é tema central da apresentação (citada pelo time) |

(D-02 descartada: SDK fora do escopo.)

