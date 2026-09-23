# ADR-0001 — Processamento assíncrono

- **Status:** aceito
- **Data:** 2026-09-22

## Contexto

O módulo antifraude recebe uma transação e devolve uma decisão (`APPROVED`, `REJECTED` ou `REVIEW`).
A avaliação consulta dados históricos (por exemplo, quantas transações o mesmo cartão fez nos últimos
minutos) e aplica um conjunto de regras que tende a crescer. O sistema precisa ser **idempotente**,
**resiliente** e **auditável**, e o contrato exige `POST /transactions` com `Idempotency-Key` e
`GET /transactions/{id}` para consultar status e decisão.

A pergunta é: o `POST` devolve a decisão na própria resposta, ou aceita a transação e decide depois?

## Opções consideradas

| Opção | Como funciona | A favor | Contra |
| :--- | :--- | :--- | :--- |
| **Síncrona** | `POST` avalia e responde `200` com a decisão | cliente simples; uma chamada | latência do cliente = latência das regras; falha de uma dependência derruba a recepção; retry do cliente reexecuta a avaliação |
| **Síncrona com timeout e fallback** | tenta decidir em até N ms; se estourar, responde `REVIEW` | resposta quase sempre imediata | dois caminhos de código para a mesma decisão; `REVIEW` por lentidão, não por risco |
| **Assíncrona com disparo direto** | `POST` publica a mensagem no broker e responde `202`, sem gravar nada; o worker cria a transação | menos código na API | sem onde registrar a `Idempotency-Key` junto da transação (reenvio gera outra avaliação); `GET` logo após o `202` devolve `404`; se o worker falhar, não fica registro de que a transação chegou; broker fora do ar = transação perdida ou recusada |
| **Assíncrona com outbox** | `POST` persiste e responde `202 Accepted` + `Location`; um worker avalia; a decisão é consultada pelo `GET` e publicada como evento | recepção desacoplada da avaliação; retry e DLQ sem afetar o cliente; escala independente | cliente precisa consultar ou ouvir o evento; consistência eventual |

## Decisão

**Processamento assíncrono.** O `POST /api/v1/transactions` valida a requisição, garante a
idempotência, grava a transação (`Received`) e a mensagem de saída na mesma transação de banco e
responde `202 Accepted` com o header `Location: /api/v1/transactions/{id}`. Um worker consome a
mensagem, aplica as regras e grava a decisão. O resultado fica disponível no
`GET /api/v1/transactions/{id}` e é publicado no evento `transaction-decided` para quem precisar reagir
(o marketplace, neste projeto).

O `POST` apenas dispara o worker, mas o disparo é **gravado antes de ser enviado**: a mensagem na outbox
é o gatilho, e é isso que o torna confiável. O próprio contrato pedido aponta nessa direção: a existência de um `GET` para "status e decisão"
pressupõe que a decisão pode não estar pronta no momento do `POST`.

## Consequências

**Positivas**

- A recepção de transações não depende da disponibilidade nem da latência da avaliação.
- Retry com backoff, DLQ e fallback acontecem no worker, sem o cliente perceber.
- API e worker escalam de forma independente.
- A idempotência fica mais simples: o `POST` só precisa garantir que a transação foi aceita uma vez.

**Negativas e mitigação**

- **Consistência eventual.** Por um curto período, a transação existe sem decisão (`Received` ou
  `Processing`). Mitigação: o `GET` expõe o `status` explicitamente, e o evento `transaction-decided`
  avisa quem precisa reagir, sem polling.
- **Mais peças** (broker, worker, outbox). Mitigação: tudo sobe com um `docker compose up`, e o fluxo
  pode ser acompanhado de ponta a ponta no painel de observabilidade.
- **O cliente não recebe a decisão na hora.** Para um checkout, isso significa uma compra
  "pendente de análise" por alguns instantes — aceitável e comum em antifraude real.
