# ADR-0004 — Idempotência e deduplicação

- **Status:** aceito
- **Data:** 2026-09-22

## Contexto

Num fluxo assíncrono com rede no meio, **repetições são inevitáveis**:

- o **cliente** reenvia uma requisição após timeout, sem saber se a primeira foi aceita;
- duas requisições **idênticas chegam ao mesmo tempo** (duplo clique, retry agressivo);
- a **outbox** entrega cada mensagem *pelo menos uma vez* — a mesma mensagem pode chegar duas vezes;
- a **reconciliação** republica compras paradas, de propósito.

Numa transação financeira, processar duas vezes significa cobrar duas vezes. O objetivo é que
**repetir qualquer operação, em qualquer ponto, produza o mesmo resultado que executá-la uma vez**.

## Opções consideradas

| Opção | Por que foi descartada ou escolhida |
| :--- | :--- |
| **Chave em cache (Redis)** | descartada — a chave e a transação ficariam em sistemas diferentes: se um gravar e o outro falhar, a garantia se perde (escrita dupla). Redis faria sentido como otimização para volume muito alto, nunca como garantia |
| **Middleware HTTP que grava a chave ao final da requisição** | descartada — a gravação da chave é separada da gravação do dado; uma queda entre as duas deixa o dado sem chave, e o reenvio duplica |
| **Chave no mesmo banco e na mesma transação do dado** | **escolhida** — o índice único do banco garante que só uma requisição vence, e a chave nunca existe sem o dado (nem o contrário) |
| **Consumidor sem deduplicação** | descartada — a entrega *pelo menos uma vez* da outbox geraria avaliações e atualizações duplicadas |
| **Checagem de estado no consumidor** | **escolhida e implementada** — uma ação já aplicada não se repete (transação decidida, compra fora de `PendingFraudCheck`) |
| **Inbox do MassTransit** | **prevista, não ativada** — as tabelas existem nas migrations; ativar exige configurar o outbox de consumer por endpoint e revalidar a interação com a unidade de trabalho. Fica como evolução |

## Decisão

Idempotência em **cinco pontos**, cada um cobrindo uma forma de repetição:

| # | Onde | Mecanismo | Cobre |
| :--- | :--- | :--- | :--- |
| 1 | `POST /api/v1/transactions` e `POST /api/v1/purchases` | header `Idempotency-Key` + hash do corpo, gravados na mesma transação do dado | reenvio do cliente, requisições simultâneas |
| 2 | consumidores | checagem de estado (ver linha 3); inbox do MassTransit previsto | reentrega da outbox |
| 3 | casos de uso | checagem de estado antes de agir (ex.: só avalia transação em `Received`/`Processing`; só atualiza compra em `PendingFraudCheck`) | qualquer reprocessamento que passe pelas camadas anteriores |
| 4 | marketplace → antifraude | `Idempotency-Key = PurchaseId` | a mesma compra submetida mais de uma vez |
| 5 | reconciliação | republica usando a mesma chave | recuperação segura de compras paradas |

### Comportamento HTTP

Segue o draft IETF `draft-ietf-httpapi-idempotency-key-header-07` e a prática do Stripe:

| Situação | Resposta |
| :--- | :--- |
| header ausente | `400 Bad Request` |
| chave nova | processa e responde normalmente (`202 Accepted` + `Location`) |
| chave repetida, mesmo corpo | **replay**: devolve o status code original, header `Location` original e `Idempotent-Replayed: true` |
| chave repetida, corpo diferente | `422 Unprocessable Entity` |
| original ainda em execução | `409 Conflict` — o cliente pode tentar de novo |

### Como funciona

- **Tabela `idempotency_keys` em cada schema** (`bookstore` e `fraud`): cada chave mora junto do dado
  que protege. Campos: chave, hash SHA-256 do corpo normalizado, **status HTTP** (`status_code`),
  **header Location** (`location_header`), corpo da resposta, id do recurso criado, data de criação.
  O replay devolve exatamente o mesmo status code + Location da resposta original, além de
  `Idempotent-Replayed: true` para que o cliente saiba que não houve novo processamento.
- **Atomicidade:** chave, entidade e mensagem da outbox entram no mesmo `SaveChanges`. Ou tudo, ou nada.
- **Concorrência:** duas requisições com a mesma chave disputam o índice único. A segunda espera o fim da
  primeira com um `lock_timeout` curto: se a primeira confirmou, a segunda recebe o **replay**; se o
  tempo esgotou, recebe `409`.
- **Validação antes da chave:** requisição inválida (corpo malformado) é rejeitada sem registrar a chave,
  para que o cliente possa corrigir e reenviar com a mesma chave.
- **Retenção:** chaves são mantidas por 24 horas e removidas por um job no Worker. Depois disso, a mesma
  chave é tratada como nova.
- **Formato da chave:** até 255 caracteres; recomendado UUID v4. Nunca dados pessoais.
- **Componente compartilhado:** o filtro HTTP e a abstração `IIdempotencyStore` são únicos e servem aos
  dois serviços; os dados de cada serviço ficam no próprio schema.

## Consequências

**Positivas**

- Qualquer mensagem pode ser reenviada, a qualquer momento, quantas vezes for preciso, com o mesmo
  resultado. É isso que torna retry, DLQ e reconciliação seguros.
- A garantia está no banco (índice único + transação), não em código que pode ter bug de corrida.
- O cliente recebe a resposta que perdeu, em vez de um erro, quando reenvia.

**Negativas e mitigação**

- **Uma escrita a mais por requisição** (a linha da chave). Mitigação: é a mesma transação, sem ida
  extra ao banco além do insert.
- **Tabela cresce.** Mitigação: retenção de 24 horas com limpeza periódica.
- **Diferença em relação ao Stripe:** lá, o resultado é salvo mesmo quando a execução falha (inclusive
  `500`). Aqui, a chave faz parte da transação do dado: se a execução falha, o rollback desfaz também a
  chave, e o cliente pode reenviar com a mesma chave. É consequência direta da atomicidade — e mais
  simples de explicar ao cliente.

## Referências

- IETF — `draft-ietf-httpapi-idempotency-key-header-07`, seção 2.7 (Error Handling).
- Stripe API — Idempotent requests.
- MassTransit — Transactional Outbox e Consumer Inbox.
