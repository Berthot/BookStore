# ADR-0007 — Estratégia de deploy (diferencial)

- **Status:** aceito
- **Data:** 2026-09-22

## Contexto

O projeto será clonado e executado por quem o avalia, em uma máquina que não controlamos. A execução
precisa funcionar de primeira, exigindo o mínimo de pré-requisitos. Além disso, a estratégia precisa
indicar como a solução iria para produção, onde API e Worker escalam por motivos diferentes: a API
por volume de requisições, o Worker pelo tamanho da fila.

## Opções consideradas

| Opção | A favor | Contra |
| :--- | :--- | :--- |
| **Containers + docker-compose** | um comando sobe tudo; só exige Docker; mesma imagem em qualquer ambiente | não é orquestrador de produção (sem autoescala, sem alta disponibilidade) |
| **Kubernetes** | autoescala, alta disponibilidade, rollout gradual; padrão de mercado para serviços | pesado para rodar localmente; manifestos exigiriam um cluster de quem avalia |
| **Serverless** (Functions/Lambda) | escala a zero; sem servidor para operar | consumidores e entrega da outbox são processos contínuos; cold start na avaliação; acopla ao provedor |
| **PaaS** (App Service, ECS…) | menos operação que Kubernetes | acopla ao provedor; avaliador não reproduz localmente |

## Decisão

**Entregue: containers com docker-compose.** Em produção: **Kubernetes**, descrito aqui, não
implementado.

### O que é entregue

- **Um Dockerfile multi-stage por aplicação** (`WebApi`, `Worker`): build com o SDK, imagem final só com
  o runtime.
- **`deploy/docker-compose.yml`**: PostgreSQL, RabbitMQ, WebApi, Worker e Aspire Dashboard, com
  *healthchecks* — as aplicações só sobem quando banco e broker estão saudáveis.
- **`deploy/.env.example`**: todas as variáveis, com valores de demonstração.
- **Migrations** aplicadas pela WebApi no startup, ligadas por configuração (ADR-0003).
- **Dados de exemplo** (livros) para os cenários de simulação funcionarem de imediato.
- **Aspire AppHost** (`apps/Aspire`) para desenvolvimento local, com as mesmas chaves de configuração
  do compose.

Execução: `docker compose -f deploy/docker-compose.yml up`.

### Como seria em produção (Kubernetes)

| Componente | Em produção |
| :--- | :--- |
| WebApi | `Deployment` com várias réplicas e autoescala por CPU/requisições (HPA) |
| Worker | `Deployment` com autoescala pelo **tamanho da fila** (KEDA, scaler de RabbitMQ) |
| Migrations | etapa dedicada do pipeline ou `Job` antes do rollout — nunca no startup |
| PostgreSQL / RabbitMQ | serviços gerenciados, um banco por serviço |
| Segredos | gerenciador de segredos, injetados como variáveis de ambiente |
| Telemetria | OTLP para um OpenTelemetry Collector e dele para o backend de observabilidade |

A mesma imagem de container roda no compose e no Kubernetes; muda apenas a configuração.

## Consequências

**Positivas**

- Quem avalia precisa apenas de Docker — nem .NET SDK, nem configuração manual.
- Imagens idênticas entre demonstração e produção reduzem surpresas no deploy.
- A separação API/Worker permite escalar cada um pelo seu próprio gargalo.

**Negativas e mitigação**

- **Compose não é produção.** Mitigação: o caminho para Kubernetes está descrito acima, e as decisões
  já tomadas (configuração por variáveis de ambiente, processos sem estado, migrations desligáveis) o
  tornam direto.
- **Duas formas de executar (compose e Aspire AppHost).** Mitigação: papéis distintos — compose é o
  contrato de execução; Aspire, a experiência de desenvolvimento — e as mesmas chaves de configuração
  nos dois.
