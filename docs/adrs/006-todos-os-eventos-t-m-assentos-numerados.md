## ADR-006: Todos os eventos têm assentos numerados

**Status:** ✅ Aceito

**Contexto:** A versão inicial do storytelling diferenciava Palestra (sem assentos, controle de vagas) de Teatro (com assentos). Após revisão, decidiu-se que todos os eventos terão assentos.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Todos com assentos** | Modelo único, ItemReserva sempre tem IngressoId, geração simplificada | Palestras não precisam de mapa complexo |
| Palestra sem assentos (controle de vagas) | Simples para workshops | Dois fluxos de reserva, IngressoId nullable, queries diferentes |
| Só Teatro (sem Palestra) | Um tipo só, nada de enum | Não atende o nicho principal (workshops, cursos) |

**Decisão:** Ambos os tipos (`Teatro` e `Palestra`) geram ingressos na criação do evento. A diferença está na numeração e distribuição: Teatro usa filas de 20 + setores VIP/Geral; Palestra usa "Assento 1" a "Assento N", todos setor Geral.

**Consequências:**
- `ItemReserva.IngressoId` é `NOT NULL` — sempre aponta para um assento
- `GerarLoteIngressos()` chamado para ambos os tipos
- Enum `TipoEvento` mantido para diferenciar a experiência visual (mapa de filas vs grid simples)
- Query de disponibilidade: `COUNT(*) FROM Ingressos WHERE Status = 0` (igual para ambos)

---

