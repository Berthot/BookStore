# ADR-0008 — Regras antifraude

- **Status:** aceito
- **Data:** 2026-09-22

## Contexto

O antifraude precisa decidir entre `APPROVED`, `REJECTED` e `REVIEW` a partir de sinais da
transação e do histórico. As decisões precisam ser **auditáveis** — explicar, a qualquer momento,
qual regra disparou, com qual versão e por quê — e o conjunto de regras precisa **crescer** sem
alterar o que já existe.

Dois tipos de risco são cobertos:

- **Fraude transacional** — pagamento não autorizado (cartão roubado, conta criada para fraudar).
- **Discrepância de padrão** — transações que fogem do comportamento do próprio cliente, incluindo
  fracionamento de valores para escapar de limites (padrão típico de lavagem de dinheiro).

## Opções consideradas

| Opção | A favor | Contra |
| :--- | :--- | :--- |
| **Uma classe por regra, em código** (`IFraudRule`) | cada regra isolada e testável; nova regra = nova classe; versão acompanha o commit | mudar um limite exige deploy |
| **Regras como dados** (tipo + parâmetros no banco) | ajuste sem deploy | exige um interpretador de regras; lógica fica difícil de testar e revisar |
| **Modelo de machine learning** | capta padrões não previstos | decisão pouco explicável; exige dados de treino que o desafio não tem |

## Decisão

**Uma classe por regra, implementando `IFraudRule`**, orquestradas por um conjunto e uma política:

- **`IFraudRule`** — `Code`, `Version` e `Evaluate(FraudContext)`, que devolve um `RuleEvaluation`
  (disparou ou não, peso, código e motivo).
- **`FraudRuleSet`** — executa **todas** as regras e soma os pesos. Registra o resultado de cada regra,
  inclusive as que não dispararam: a auditoria precisa provar também o que foi verificado e passou.
- **`DecisionPolicy`** — converte a pontuação em resultado: `score ≥ RejectThreshold → Rejected`;
  `score ≥ ReviewThreshold → Review`; senão `Approved`.
- **Regras puras** — nenhuma regra acessa banco. O caso de uso monta o `FraudContext` com os sinais
  históricos já calculados (contagens, médias); as regras só leem.
- **Parâmetros como constantes na classe** — limites e pesos ficam junto da lógica, e a `Version` sobe
  quando qualquer um muda. Assim a versão gravada nunca descola do comportamento.
- **Motivo com código estável** — cada motivo tem um código (`HIGH_VALUE_DIGITAL`) e um texto. Testes
  verificam o código; o texto pode mudar sem quebrar nada.
- **Sinal não é prova** — regras de discrepância pesam para `Review`, não para `Rejected`. Um desvio de
  padrão justifica investigação; combinado com outras regras, pode levar à rejeição.

## Catálogo de regras

Limites e pesos definitivos são fixados na implementação; cada regra existe para produzir ao menos
um cenário da simulação.

| Regra | Código | Tipo | Dispara quando | Tende a | Cenário |
| :--- | :--- | :--- | :--- | :--- | :---: |
| `HighValueDigitalRule` | `HIGH_VALUE_DIGITAL` | transacional | entrega digital acima de um valor | somar para rejeição | S2 |
| `NewCustomerHighAmountRule` | `NEW_CUSTOMER_HIGH_AMOUNT` | transacional | cliente sem histórico com valor alto | somar para rejeição | S2 |
| `BulkQuantityRule` | `BULK_QUANTITY` | transacional | muitas unidades do mesmo item | revisão | S3 |
| `CardVelocityRule` | `CARD_VELOCITY` | transacional | muitas transações do mesmo cartão em pouco tempo | rejeição | S5 |
| `AmountDeviationRule` | `AMOUNT_DEVIATION` | discrepância | valor muito acima da média do próprio cliente (com histórico mínimo) | revisão | S10 |
| `StructuringRule` | `STRUCTURING` | discrepância | várias transações do cliente logo abaixo de um limite, em janela curta | revisão | S11 |

### Sinais que o `FraudContext` recebe

| Sinal | Usado por | Como é obtido |
| :--- | :--- | :--- |
| `Amount`, `Delivery`, `ItemCount`, `Channel`, `OccurredAt` | várias | da própria transação |
| `IsNewCustomer` | `NEW_CUSTOMER_HIGH_AMOUNT` | cliente sem transações anteriores |
| `RecentTransactionsWithSameCard` | `CARD_VELOCITY` | contagem por `payment_fingerprint` na janela |
| `CustomerAverageAmount`, `CustomerTransactionCount` | `AMOUNT_DEVIATION` | agregados do histórico do cliente |
| `RecentJustBelowThresholdCount` | `STRUCTURING` | contagem do cliente na faixa abaixo do limite, na janela |

## Consequências

**Positivas**

- Toda decisão é explicável: regras avaliadas, versões, pesos e motivos ficam gravados.
- Nova regra = nova classe + registro; `FraudRuleSet` e `DecisionPolicy` não mudam.
- Regras sem I/O: testes unitários cobrem cada cenário de disparo e de não disparo em milissegundos.

**Negativas e mitigação**

- **Ajuste de limite exige deploy.** Mitigação futura: parâmetros externos com a versão derivada de um
  hash dos parâmetros, preservando a auditoria.
- **Padrões não previstos passam.** Mitigação futura: um score de modelo de ML entra como mais uma
  `IFraudRule`, dentro do mesmo conjunto — a arquitetura não muda, e a explicabilidade das demais
  regras se mantém.
- **Sinais de provedores externos** (bureau, lista de bloqueio, geolocalização) entram pelo `FraudContext`,
  obtidos no caso de uso com timeout e circuit breaker — nunca dentro da regra. Provedor indisponível =
  sinal "indisponível", e a regra registra que não pôde avaliar.
- **Evolução documentada:** conluio entre comprador e vendedor no marketplace (vendedor anuncia item
  superfaturado e compra de si mesmo) exige modelar vendedores; fica fora do escopo atual.
