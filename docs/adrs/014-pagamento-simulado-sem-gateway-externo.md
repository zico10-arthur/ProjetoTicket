## ADR-014: Pagamento Simulado (Sem Gateway Externo)

**Status:** ✅ Aceito

**Contexto:**
O sistema precisa processar pagamentos para reservas de ingressos, mas a integração
com gateways reais (Stripe, PagSeguro, Mercado Pago) adiciona complexidade de
homologação, custos de transação e dependência externa. Durante a fase de MVP, o
foco está na validação do fluxo completo de compra — não na liquidação financeira
real.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|---|---|---|
| **Pagamento simulado (Mock interno)** | Zero dependência externa, fluxo validado de ponta a ponta, testes sem mock de gateway | Dinheiro não é movimentado, não serve para produção real |
| Gateway real (Stripe/PagSeguro) | Dinheiro real, conformidade PCI, relatórios fiscais | Complexidade de homologação, custos, webhooks, ambientes sandbox separados |
| Adiar pagamento (só reserva) | MVP mais rápido | Não valida o fluxo completo — check-out é parte crítica da UX |

**Decisão:**
Pagamento **simulado** como mock interno. Toda reserva paga gera um registro na
tabela `Pagamentos` com `Status = Confirmado` e `Metodo = "Simulado"`. Eventos
gratuitos têm `ValorPago = 0` e confirmam diretamente, sem etapa de pagamento. O
modelo de dados e as transações atômicas (`CriarComTransacao` no
`PagamentoService`) já foram projetados para acomodar um gateway real no futuro
— basta trocar a implementação concreta.

**Consequências:**

Prós:
- Fluxo de compra completo (selecionar assentos → revisar → "pagar" → confirmar)
  validado em todas as camadas (Controller → Service → Repository → Banco)
- Testes de pagamento (`PagamentoTests`) verificam o fluxo real com mock
  de repositório, sem dependência de API externa
- Tabela `Pagamentos` com schema pronto para gateway real (`Metodo`,
  `Status`, `ValorPago`, `DataPagamento`)
- Transição para gateway real exige apenas nova implementação de
  `IPagamentoRepository` — `PagamentoService` e `ReservaService` não mudam
- Eventos gratuitos (`PrecoPadrao = 0`) são tratados uniformemente
  (`ValorPago = 0`) sem bifurcação de código

Contras:
- Nenhuma validação financeira real — risco de descobrir problemas de
  integração apenas na troca para gateway real
- Ausência de callbacks/webhooks de confirmação — o modelo atual assume
  pagamento síncrono e instantâneo
- Comprovantes de pagamento são apenas registros internos, sem validade
  fiscal ou jurídica
- Métricas de conversão real (taxa de abandono no pagamento) não podem
  ser medidas com fidelidade

---

