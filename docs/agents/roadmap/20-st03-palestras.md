# 20 — ST-03: Eventos de Pequeno Porte — Foco em Palestras

> **Origem:** [`storytelling.md#st-03-eventos-de-pequeno-porte`](../../storytelling.md#st-03-eventos-de-pequeno-porte)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Criar e divulgar eventos de forma simples, sem depender de plataformas complexas.

---

## 20.1 História

**Como** um vendedor,
**Quero** criar eventos do tipo Palestra (com assentos numerados e controle de vagas),
**Para** vender ingressos para workshops, cursos e meetups de forma simples.

---

## 20.2 Modelo de Dados

```csharp
public enum TipoEvento
{
    Teatro = 0,
    Palestra = 1
}
```

### Ambos os tipos geram ingressos

| Característica | Teatro (0) | Palestra (1) |
|---|---|---|
| Geração de ingressos | Sim | Sim |
| Distribuição | 10% VIP (preço × 1.5), 90% Geral | 100% Geral |
| Numeração | `"Fila X \| Assento Y"` (filas de 20) | `"Assento 1"` a `"Assento N"` |
| Mapa visual | Sim | Sim (mais simples) |
| Foco da plataforma | Secundário | **Principal** |

---

## 20.3 Geração de Ingressos

```csharp
private List<Ingresso> GerarLoteIngressos(Evento evento)
{
    if (evento.Tipo == TipoEvento.Teatro)
        return GerarTeatro(evento);   // 10% VIP, 90% Geral, filas de 20
    else
        return GerarPalestra(evento); // 100% Geral, assentos 1..N
}

private List<Ingresso> GerarPalestra(Evento evento)
{
    var ingressos = new List<Ingresso>();
    for (int i = 1; i <= evento.CapacidadeTotal; i++)
    {
        ingressos.Add(new Ingresso
        {
            Id = Guid.NewGuid(),
            EventoId = evento.Id,
            Preco = evento.PrecoPadrao,
            Setor = "Geral",
            Posicao = $"Assento {i}",
            Status = 0
        });
    }
    return ingressos;
}
```

---

## 20.4 Lógica de Disponibilidade

```sql
-- Ambos os tipos: contar ingressos não vendidos
SELECT COUNT(*) AS Disponiveis
FROM Ingressos
WHERE EventoId = @eventoId AND Status = 0;
```

---

## 20.5 Criação da Reserva

```
POST /api/reserva/criar
Auth: JWT
```

```json
{
    "eventoId": "a1b2c3d4-...",
    "itens": [
        { "cpfParticipante": "12345678901", "ingressoId": "i1i2i3i4-..." },
        { "cpfParticipante": "98765432100", "ingressoId": "i5i6i7i8-..." }
    ],
    "cupomCodigo": null
}
```

**Regra:** `IngressoId` sempre preenchido. O comprador seleciona assentos específicos no mapa visual (Palestra: grid simples; Teatro: filas e setores).

---

## 20.6 Regras de Negócio

```
1. Criação do evento:
   → SEMPRE executar GerarLoteIngressos()
   → Teatro → VIP/Geral com filas
   → Palestra → Assentos 1..N, todos Setor "Geral"

2. Reserva:
   → Cada ItemReserva: IngressoId = assento escolhido
   → Validar: Ingresso.Status == 0 (Livre) para cada IngressoId
   → Se nenhum livre: 400 "Evento esgotado"

3. Cancelamento:
   → Ingressos voltam a Status 0 (Livre)
   → ItensReserva marcados Reembolsado = true
```

## 20.7 Respostas

| Código | Caso |
|--------|------|
| `201 Created` | Reserva criada |
| `400 Bad Request` | Ingresso já reservado: "Assento não está disponível" |
| `400 Bad Request` | Evento esgotado |
