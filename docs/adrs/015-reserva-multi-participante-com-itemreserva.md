## ADR-015: Reserva Multi-Participante com ItemReserva

**Status:** ✅ Aceito

**Contexto:**
O sistema precisa permitir que um comprador adquira ingressos para si e para
terceiros em uma única transação. Cada ingresso pode ser atribuído a uma pessoa
diferente, identificada por CPF. Isso é essencial para eventos onde o comprador
adquire ingressos para amigos, familiares ou colegas sem que cada pessoa precise
criar uma conta no sistema.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|---|---|---|
| **ItemReserva com CPF por item** | CPFs independentes, reembolso granular, dados de participantes para o organizador | Schema mais complexo, validação de CPF por item |
| Reserva com único CPF (comprador) | Schema simples, só um campo CPF | Não atende compra para terceiros, reembolso total ou nada |
| Participantes como tabela separada | Normalizado, sem limite de participantes | Mais tabelas, joins extras, complexidade desnecessária para limite de 4 |

**Decisão:**
Cada `Reserva` contém uma coleção de até 4 `ItemReserva`, cada um com
`CpfParticipante` independente. Os CPFs dos participantes **não precisam estar
cadastrados** no sistema — o organizador só precisa do CPF para identificação no
dia do evento. O cupom de desconto, quando aplicado, incide sobre o valor total
da reserva (soma dos preços unitários), não por item.

**Consequências:**

Prós:
- Um comprador adquire ingressos para até 4 pessoas em uma única transação
- Cada `ItemReserva` tem `PrecoUnitario` e flag `Reembolsado` independentes —
  base para reembolso granular no futuro (ex: cancelar 2 de 4 ingressos)
- CPFs de participantes não inflam a tabela `Usuarios` — sistema permanece
  enxuto
- Validação de CPF duplicado na mesma reserva impede compra acidental de 2
  ingressos para a mesma pessoa
- Limite de 4 itens por reserva (`LimiteMaximoItens`) é uma constante,
  facilmente ajustável
- Cupom aplicado sobre o total da reserva simplifica o cálculo — desconto é
  único, não por item

Contras:
- Limite fixo de 4 participantes — eventos familiares ou compras corporativas
  exigem múltiplas reservas
- Schema com relação 1:N entre Reserva e ItemReserva adiciona complexidade às
  queries de listagem e relatórios
- `CpfParticipante` não validado contra Receita Federal (só validação de
  dígitos verificadores) — CPFs fictícios podem ser informados
- Cupom sobre o total da reserva significa que itens mais caros e mais baratos
  recebem o mesmo percentual de desconto — não há rateio proporcional
- Migração de reembolso granular (itens individuais) exigirá alteração no
  fluxo de cancelamento atual, que opera sobre a reserva inteira

---

> **Formato baseado em:** [ADR GitHub](https://adr.github.io/) — Michael Nygard  
> **Próximo:** Implementação da Sprint 1 (specs 120, 130, 150, 160, 180)
