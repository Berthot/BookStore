# ADR-0003 — Banco de dados

- **Status:** aceito
- **Data:** 2026-09-22

## Contexto

O banco sustenta as três garantias do sistema:

- **Idempotência** — a `Idempotency-Key` precisa ser única, e a verificação tem de ser atômica com a
  gravação da transação (ADR-0004).
- **Resiliência** — a outbox exige gravar estado e mensagem **na mesma transação** (ADR-0002).
- **Auditabilidade** — decisões são *append-only* e precisam ser consultáveis ("por que esta transação
  foi rejeitada?", "quantas vezes a regra X disparou?").

As regras antifraude também consultam histórico — por exemplo, quantas transações o mesmo cartão fez
numa janela de tempo — o que pede índices compostos e consultas por intervalo.

## Opções consideradas

| Opção | A favor | Contra |
| :--- | :--- | :--- |
| **PostgreSQL** | transações ACID; índice único como garantia de idempotência; schemas para isolar contextos; `jsonb` quando for útil; EF Core maduro (Npgsql); open source e leve em container | escala de escrita horizontal exige mais trabalho (particionamento, réplicas) |
| **SQL Server** | mesmas garantias relacionais; muito comum em .NET | imagem mais pesada; licenciamento fora do Developer/Express |
| **MongoDB** | modelo de documento flexível; transações multi-documento disponíveis | a unicidade e a atomicidade entre coleções exigem mais cuidado de modelagem; auditoria e consultas analíticas ficam menos naturais; outbox e inbox do MassTransit são mais usuais sobre EF Core relacional |
| **DynamoDB / Cassandra** | escala de escrita muito alta | acoplamento a provedor (Dynamo) ou operação complexa (Cassandra); consultas por janela de tempo exigem modelagem específica; excessivo para o volume do desafio |

## Decisão

**PostgreSQL**, acessado por **EF Core**, com **dois schemas** — `bookstore` (marketplace) e `fraud`
(antifraude):

- **dois `DbContext`** (`BookStoreDbContext`, `FraudDbContext`), cada um com schema padrão próprio,
  pasta de migrations própria e tabela `__EFMigrationsHistory` no próprio schema;
- mapeamentos em `IEntityTypeConfiguration<T>`, separados das entidades e aplicados por contexto
  (filtro por namespace);
- migrations **geradas pela CLI** (`dotnet ef migrations add`), nunca escritas à mão, e aplicadas no
  startup da API (`MigrateAsync`, só as pendentes) quando habilitado por configuração;
- cada schema guarda suas próprias tabelas de idempotência, outbox e inbox;
- avaliações de regra (`RuleEvaluation`) em tabela própria, e não em `jsonb`, para permitir consultas
  de auditoria por regra;
- índice composto em `(payment_fingerprint, occurred_at)` para as regras de velocidade.

## Consequências

**Positivas**

- A idempotência é garantida pelo banco (índice único), não por código.
- Estado, chave de idempotência e mensagem de saída são gravados atomicamente.
- Cada contexto enxerga só o próprio schema; separá-los em bancos distintos no futuro é uma troca de
  connection string.
- A auditoria é consultável com SQL comum.

**Negativas e mitigação**

- **Um servidor para dois contextos.** Em produção, cada serviço teria seu próprio banco. Mitigação: a
  separação por schema e por `DbContext` já prepara essa divisão.
- **Migrations no startup** não são o modelo de produção. Mitigação: o comportamento é ligado por
  configuração; em produção, as migrations seriam uma etapa dedicada do pipeline. Desde o EF Core 9,
  `MigrateAsync` usa lock no banco contra aplicações concorrentes.
- **Crescimento das tabelas de auditoria.** Mitigação futura: particionamento por data em
  `assessments` e `rule_evaluations`.
