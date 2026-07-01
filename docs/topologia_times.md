# Topologia de Times (Team Topologies)

---

## Mapeamento dos 4 Tipos de Time para o Contexto do Projeto SoldOut Tickets

| Tipo de Time | Definição (Team Topologies) | Aplicação no Projeto | Responsabilidades |
|:---|:---|:---|:---|
| **🟢 Stream-Aligned** | Time alinhado a um fluxo contínuo de valor de negócio, com autonomia para entregar features de ponta a ponta. | **Time de Funcionalidades de Ingresso e Reserva** — focado no fluxo principal: cadastro de eventos → compra de ingressos → reserva → checkout. | Desenvolver e manter os endpoints de Evento, Ingresso, Reserva e Checkout. Responsável por toda a jornada do comprador e vendedor no sistema de tickets. |
| **🔵 Platform** | Time que constrói e mantém uma plataforma interna (APIs, ferramentas, infraestrutura) usada pelos times Stream-Aligned para acelerar entregas. | **Time de Infraestrutura e Dados** — mantém o Docker, DbUp migrations, Dapper/ConnectionFactory, Hangfire e pipeline de CI/CD como serviços internos consumidos pelos demais times. | Gerenciar containerização (Docker), banco de dados (migrations DbUp), fábrica de conexões Dapper, jobs recorrentes (Hangfire) e esteira de deploy. Garantir que os times de funcionalidade não precisem se preocupar com infra. |
| **🟠 Enabling** | Time que auxilia outros times a superar gaps de conhecimento, capacitando-os com práticas, ferramentas e mentoria temporária. | **Time de Segurança e Qualidade** — atua de forma transversal, entrando nas squads sob demanda para capacitar em práticas de segurança (JWT, BCrypt, Rate Limiting) e qualidade (testes, code review). | Auditar segurança de endpoints, definir gates de segurança, orientar code reviews, configurar ferramentas de SAST, capacitar times em testes automatizados e TDD. Time temporário que se dissolve quando o conhecimento é internalizado. |
| **🔴 Complicated-Subsystem** | Time dedicado a um subsistema de alta complexidade que exige conhecimento especializado e não pode ser diluído entre times Stream-Aligned. | **Time de Pagamentos e Reembolsos** — focado exclusivamente no motor financeiro: processamento de pagamentos, reembolsos, transações atômicas multi-tabela e integração com gateway externo. | Manter a lógica de pagamento/reembolso (Spec 50, 170), garantir atomicidade das transações (BEGIN/COMMIT/ROLLBACK), isolar integração com gateway de pagamento (adapter pattern) e garantir idempotência e consistência financeira. |

---

## Diagrama de Interação

```
                    ┌──────────────────────────┐
                    │     🔵 Platform Team      │
                    │  Docker • DbUp • Dapper   │
                    │  Hangfire • CI/CD          │
                    └──────────┬───────────────┘
                               │ consome
          ┌────────────────────┼────────────────────┐
          ▼                    ▼                     ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐
│ 🟢 Stream-Aligned│  │ 🟢 Stream-Aligned│  │ 🔴 Complicated-Sub. │
│ Ingresso/Reserva │  │     Eventos      │  │  Pagamento/Reembolso│
└────────┬────────┘  └────────┬────────┘  └──────────┬──────────┘
         │                    │                      │
         └────────────────────┼──────────────────────┘
                              │ capacita (sob demanda)
                              ▼
                    ┌──────────────────────────┐
                    │   🟠 Enabling Team        │
                    │  Segurança • Qualidade    │
                    │  JWT • BCrypt • SAST      │
                    └──────────────────────────┘
```
