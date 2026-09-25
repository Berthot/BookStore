# contexto/

Plano de trabalho usado para orquestrar o agente de IA que implementou este projeto.

- `trabalho/01-entrada/` — instruções ao agente: fonte da verdade, ordem, padrões .NET.
- `trabalho/epics/` — épicos, stories e tasks com critérios de aceite. Sincronizados com o meu vault pelo
  Karawara (`.karawara/`).

**Nota honesta:** o agente fechou o primeiro épico como concluído. A revisão manual que fiz depois encontrou
defeitos (outbox desligado com perda de mensagem, consumers engolindo falhas, lacunas de idempotência) que viraram
novas rodadas de correção — descritas no README principal, seção "Uso de IA".
Lição: para trabalho delegado a IA, "concluído" só vale com prova executável.
