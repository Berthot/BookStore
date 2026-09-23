# ADR-0006 — Versionamento da API

- **Status:** aceito
- **Data:** 2026-09-22

## Contexto

A API é consumida por sistemas de terceiros (lojas que submetem transações ao antifraude). Um contrato
publicado não pode mudar de forma incompatível sem aviso: um campo removido ou renomeado quebra quem
já integrou. É preciso uma forma de evoluir o contrato sem quebrar os clientes existentes.

## Opções consideradas

| Opção | Exemplo | A favor | Contra |
| :--- | :--- | :--- | :--- |
| **Segmento de URL** | `/api/v1/transactions` | visível; fácil de testar (curl, navegador, `.http`); simples de rotear em gateway e de separar em documentação | a versão faz parte do endereço do recurso |
| **Header** | `api-version: 1` | URL estável | invisível em logs e testes manuais; esquecer o header leva a comportamento implícito |
| **Media type** | `Accept: application/vnd.bookstore.v1+json` | semanticamente preciso | complexo para clientes e ferramentas |
| **Query string** | `?api-version=1` | simples | mistura versão com parâmetros de consulta; cache e logs ficam ambíguos |

## Decisão

**Versão no segmento de URL**: todas as rotas ficam sob `/api/v1/`. Na aplicação, um grupo de rotas
(`MapGroup("/api/v1")`) concentra o prefixo — os endpoints não repetem a versão.

Política de evolução:

- **Mudança compatível** (novo campo opcional, novo endpoint) → entra na `v1`.
- **Mudança incompatível** (remover ou renomear campo, mudar semântica) → nova versão (`v2`), com a
  `v1` mantida durante um período de transição anunciado.

O contrato documenta explicitamente o *base path* (`/api/v1`) e os caminhos completos dos endpoints
exigidos (`POST /api/v1/transactions`, `GET /api/v1/transactions/{id}`).

## Consequências

**Positivas**

- Qualquer pessoa vê a versão em uso num log, num trace ou num comando curl.
- A separação entre versões fica explícita no código e na documentação OpenAPI.

**Negativas e mitigação**

- **Versão acoplada ao endereço.** Mitigação: só muda em quebra de contrato, que é rara; mudanças
  compatíveis nunca geram versão nova.
- **Manter duas versões custa.** Mitigação: prazo de transição definido, e versões antigas removidas
  ao fim dele.
