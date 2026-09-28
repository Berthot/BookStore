# ADR-0002 — Mensageria

- **Status:** aceito
- **Data:** 2026-09-22

## Contexto

O processamento é assíncrono (ADR-0001). São três mensagens, cada uma processada **uma vez por um
consumidor**:

| Mensagem | Produtor | Consumidor | Efeito |
| :--- | :--- | :--- | :--- |
| `purchase-placed` | marketplace (API) | worker | submete a transação ao antifraude |
| `transaction-submitted` | antifraude (API) | worker | avalia a transação |
| `transaction-decided` | antifraude (worker) | worker | atualiza a compra |

O mecanismo precisa oferecer **retry com backoff**, **DLQ**, integração com a **outbox** e com o
**tracing**, e subir facilmente na máquina de quem avalia o projeto.

## Opções consideradas — broker

| Opção | A favor | Contra |
| :--- | :--- | :--- |
| **RabbitMQ** | modelo de fila de trabalho (uma mensagem, um consumidor) — exatamente o caso; DLQ nativa via dead-letter exchange; roda com um container | não guarda histórico para replay |
| **Kafka** | log durável, replay, alto volume, streaming | modelo de log em vez de fila de trabalho; DLQ e retry por conta da aplicação; mais pesado para rodar localmente; excessivo para três mensagens |
| **SQS/SNS** | gerenciado, sem operação | acopla à AWS; rodar localmente exige emulador; o avaliador precisaria de mais que Docker |

## Opções consideradas — biblioteca

| Opção | A favor | Contra |
| :--- | :--- | :--- |
| **Cliente RabbitMQ direto + outbox própria** | controle total; nenhuma dependência | reescrever retry, DLQ, inbox, outbox e propagação de trace — muito código de infraestrutura para o escopo |
| **MassTransit v8** | outbox e inbox no EF Core, retry com backoff, filas `_error`, OpenTelemetry nativo; amplamente usado em .NET | a partir da v9 o licenciamento é comercial |
| **Wolverine / Rebus** | alternativas open source com outbox | menor familiaridade de mercado; mesma função |

## Decisão

**RabbitMQ como broker e MassTransit v8 como biblioteca**, com:

- **Bus Outbox** no EF Core — a mensagem é gravada na mesma transação da mudança de estado;
- **consumidor idempotente por checagem de estado** — reentregas não reprocessam o que já foi decidido
  (o Consumer Inbox do MassTransit fica como evolução: tabelas criadas, não ativado);
- **retry com backoff** em intervalos crescentes;
- **filas `_error`** como DLQ para mensagens que esgotam as tentativas.

A versão é fixada em `8.*`.

## Consequências

**Positivas**

- Outbox, retry, DLQ e tracing vêm de um único componente testado, em vez de código próprio.
- O contexto de trace atravessa a fila: uma compra aparece como um único trace no painel.
- Toda a configuração fica em uma extension dedicada (`MassTransitExtensions`), documentada.

**Negativas e mitigação**

- **Licenciamento.** A v9 é comercial; a v8 é Apache 2.0, com correções de segurança previstas até o
  fim de 2026. É adequada ao escopo deste desafio. Em produção, a escolha seria reavaliada entre a
  licença da v9, Wolverine, Rebus ou uma outbox própria — o código de domínio não muda, porque só a
  extension de mensageria conhece o MassTransit.
- **Um bus outbox por bus (limitação da v8).** O MassTransit 8 suporta um bus outbox de EF por bus;
  com dois `DbContext` no mesmo processo isso não fecha. A solução foi dividir o monólito em três
  processos (ADR-0010): cada processo tem exatamente um `DbContext` e uma outbox.
- **Entrega pelo menos uma vez.** A outbox pode entregar a mesma mensagem mais de uma vez.
  Mitigação: checagem de estado antes de agir (ADR-0004).
- **Sem replay de histórico.** O RabbitMQ não guarda mensagens já consumidas. Mitigação: o histórico
  de decisões fica no banco (`Assessment` é append-only), não no broker.
