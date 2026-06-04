# 30 — ST-04: Reserva com Múltiplos Participantes (ItemReserva)

> **Origem:** [`storytelling.md#st-04-reserva-com-múltiplos-participantes`](../../storytelling.md#st-04-reserva-com-múltiplos-participantes)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Gerenciar inscrições sem planilhas: uma compra cobre múltiplos participantes.

---

## 30.1 História

**Como** um comprador,
**Quero** comprar ingressos para mim e para outras pessoas em uma única reserva,
**Para** não precisar fazer múltiplas compras para o mesmo evento.

---

## 30.2 Entidade ItemReserva

```csharp
public class ItemReserva
{
    public Guid Id { get; private set; }
    public Guid ReservaId { get; private set; }
    public string CpfParticipante { get; private set; }   // CPF de quem vai usar o ingresso
    public Guid IngressoId { get; private set; }            // assento escolhido (sempre preenchido)
    public decimal PrecoUnitario { get; private set; }
    public bool Reembolsado { get; private set; }
}
```

### SQL

```sql
CREATE TABLE ItensReserva (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReservaId       UNIQUEIDENTIFIER NOT NULL,
    CpfParticipante VARCHAR(14)      NOT NULL,
    IngressoId      UNIQUEIDENTIFIER NOT NULL,
    PrecoUnitario   DECIMAL(10,2)    NOT NULL,
    Reembolsado     BIT              NOT NULL DEFAULT 0,
    CONSTRAINT FK_ItensReserva_Reservas FOREIGN KEY (ReservaId)
        REFERENCES Reservas(Id),
    CONSTRAINT FK_ItensReserva_Ingressos FOREIGN KEY (IngressoId)
        REFERENCES Ingressos(Id)
);
```

---

## 30.3 Regra Principal: Limite de 4 Participantes por Reserva

```
Uma reserva contém de 1 a 4 ItemReserva.
Cada ItemReserva representa 1 ingresso para 1 CPF.
O comprador logado informa o CPF de cada participante.

Exemplo: João (logado) compra para ele e mais 3 amigos:
  → Item 1: CpfParticipante = CPF do João
  → Item 2: CpfParticipante = CPF da Maria
  → Item 3: CpfParticipante = CPF do Pedro
  → Item 4: CpfParticipante = CPF da Ana
  → Total: 4 itens (limite máximo)
```

**Importante:** O CPF do comprador logado NÃO é automaticamente incluído. O comprador precisa adicionar explicitamente seu próprio CPF como um dos itens, se quiser um ingresso para si.

---

## 30.4 Endpoint — Criar Reserva

```
POST /api/reserva/criar
Auth: JWT (qualquer perfil)
Content-Type: application/json
```

### Request (exemplo com 4 participantes — limite máximo)

```json
{
    "eventoId": "a1b2c3d4-...",
    "itens": [
        { "cpfParticipante": "11122233344", "ingressoId": "a01-a01-..." },
        { "cpfParticipante": "55566677788", "ingressoId": "a02-a02-..." },
        { "cpfParticipante": "99988877766", "ingressoId": "a03-a03-..." },
        { "cpfParticipante": "33344455566", "ingressoId": "a04-a04-..." }
    ],
    "cupomCodigo": "PROMO10"
}
```

### Response 201

```json
{
    "reservaId": "r1r2r3r4-...",
    "usuarioCpf": "99988877766",
    "eventoId": "a1b2c3d4-...",
    "itens": [
        { "cpfParticipante": "11122233344", "ingressoId": "a01-a01-...", "precoUnitario": 50.00 },
        { "cpfParticipante": "55566677788", "ingressoId": "a02-a02-...", "precoUnitario": 50.00 },
        { "cpfParticipante": "99988877766", "ingressoId": "a03-a03-...", "precoUnitario": 50.00 },
        { "cpfParticipante": "33344455566", "ingressoId": "a04-a04-...", "precoUnitario": 50.00 }
    ],
    "valorTotal": 200.00,
    "desconto": 20.00,
    "valorFinalPago": 180.00
}
```

---

## 30.5 Validações

| Regra | Erro |
|-------|------|
| `itens.Count < 1` | 400 "É necessário pelo menos 1 participante" |
| `itens.Count > 4` | 400 "Limite máximo de 4 participantes por reserva" |
| `CpfParticipante` duplicado na mesma requisição | 400 "CPF {x} informado mais de uma vez" |
| `CpfParticipante` já tem reserva ativa neste evento | 409 "CPF {x} já possui reserva neste evento" |
| CPF inválido (formato/dígitos) | 400 "CPF {x} inválido" |
| `ingressoId` não pertence ao evento | 400 "Ingresso não pertence a este evento" |
| `ingressoId` com Status != 0 (Livre) | 409 "Assento já reservado ou vendido" |

---

## 30.6 Regras de Negócio

```
1. CPFs dos participantes:
   → NÃO precisam estar cadastrados no sistema
   → O comprador digita manualmente cada CPF
   → Cada CPF é vinculado a um ingresso (assento) específico

2. Cálculo de preço:
   → Cada ItemReserva.PrecoUnitario = preço do ingresso escolhido
   → Se Setor == "VIP" (apenas Teatro) → PrecoUnitario = PrecoPadrao * 1.5
   → Se Setor == "Geral" → PrecoUnitario = PrecoPadrao

3. Cupom (se informado):
   → Validar cupom (ativo, não expirado, ValorMinimo, mesmo VendedorId)
   → ValorTotal = SUM(ItemReserva.PrecoUnitario)
   → Desconto = ValorTotal * Cupom.PorcentagemDesconto / 100
   → ValorFinalPago = MAX(0, ValorTotal - Desconto)

4. Transação:
   → INSERT Reserva
   → N INSERT ItensReserva (1 a 4)
   → UPDATE Ingressos SET Status = 1 (Reservado) para cada IngressoId
   → Tudo dentro de BEGIN TRANSACTION / COMMIT
```
