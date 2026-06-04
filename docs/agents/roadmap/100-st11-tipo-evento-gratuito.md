# 100 — ST-11: Evento com Tipo (Teatro/Palestra) e Gratuito/Pago

> **Origem:** [`storytelling.md#st-11-evento-com-tipo`](../../storytelling.md#st-11-evento-com-tipo)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Criar eventos de forma simples (Palestra) ou profissional (Teatro) e permitir eventos gratuitos sem barreiras.

---

## 100.1 História

**Como** um vendedor,
**Quero** definir se meu evento é do tipo Teatro ou Palestra e se é gratuito ou pago,
**Para** configurar o evento conforme sua natureza.

---

## 100.2 Endpoint — Criar Evento

```
POST /api/evento/criar
Auth: JWT (role=Vendedor)
Content-Type: application/json
```

### Request

```json
{
    "nome": "Workshop .NET 9",
    "tipo": 1,
    "capacidadeTotal": 50,
    "dataEvento": "2026-07-15T19:00:00",
    "precoPadrao": 0,
    "descricao": "Workshop prático para iniciantes",
    "local": "Auditório Central"
}
```

**Campos:**

| Campo | Tipo | Obrigatório | Valores |
|-------|------|-------------|---------|
| `nome` | string | Sim | Não vazio |
| `tipo` | int | Sim | `0` = Teatro, `1` = Palestra |
| `capacidadeTotal` | int | Sim | > 0 |
| `dataEvento` | datetime | Sim | Data futura |
| `precoPadrao` | decimal | Sim | `0` = gratuito, `> 0` = pago |
| `descricao` | string | Não | — |
| `local` | string | Não | — |

---

## 100.3 Comparativo dos Tipos (ambos com assentos numerados)

| Característica | Teatro (0) | Palestra (1) |
|---|---|---|
| Geração de ingressos | Sim | Sim |
| Distribuição | 10% VIP, 90% Geral | 100% Geral |
| Preço VIP | `PrecoPadrao * 1.5` | — (sem setor VIP) |
| Numeração | `"Fila X \| Assento Y"` (filas de 20) | `"Assento 1"` a `"Assento N"` |
| Mapa visual | Filas e setores coloridos | Grid simples numerado |
| Gratuito suportado | Sim | Sim |

---

## 100.4 Lógica de Criação

```csharp
public Evento CriarEvento(EventoRequestDTO dto, string vendedorId)
{
    // 100.4.1 Validar data futura
    if (dto.DataEvento <= DateTime.Now)
        throw new DataEventoInvalidaException();

    // 100.4.2 Criar entidade
    var evento = new Evento
    {
        Id = Guid.NewGuid(),
        VendedorId = vendedorId,
        Nome = dto.Nome,
        Tipo = (TipoEvento)dto.Tipo,
        CapacidadeTotal = dto.CapacidadeTotal,
        DataEvento = dto.DataEvento,
        PrecoPadrao = dto.PrecoPadrao,
        Descricao = dto.Descricao,
        Local = dto.Local,
        DataCriacao = DateTime.Now
    };

    _eventoRepo.Inserir(evento);

    // 100.4.3 Ambos os tipos geram ingressos
    var ingressos = GerarLoteIngressos(evento);
    _ingressoRepo.InserirLote(ingressos);

    return evento;
}
```

---

## 100.5 Geração de Ingressos por Tipo

```csharp
private List<Ingresso> GerarLoteIngressos(Evento evento)
{
    return evento.Tipo == TipoEvento.Teatro
        ? GerarTeatro(evento)
        : GerarPalestra(evento);
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

private List<Ingresso> GerarTeatro(Evento evento)
{
    int vip = (int)(evento.CapacidadeTotal * 0.1);
    int geral = evento.CapacidadeTotal - vip;
    var ingressos = new List<Ingresso>();
    int fila = 1, assento = 1;

    for (int i = 0; i < vip; i++)
    {
        ingressos.Add(new Ingresso
        {
            Id = Guid.NewGuid(),
            EventoId = evento.Id,
            Preco = evento.PrecoPadrao * 1.5m,
            Setor = "VIP",
            Posicao = $"Fila {fila} | Assento {assento}",
            Status = 0
        });
        if (++assento > 20) { fila++; assento = 1; }
    }
    for (int i = 0; i < geral; i++)
    {
        ingressos.Add(new Ingresso
        {
            Id = Guid.NewGuid(),
            EventoId = evento.Id,
            Preco = evento.PrecoPadrao,
            Setor = "Geral",
            Posicao = $"Fila {fila} | Assento {assento}",
            Status = 0
        });
        if (++assento > 20) { fila++; assento = 1; }
    }
    return ingressos;
}
```

---

## 100.6 Fluxo de Reserva (Gratuito vs Pago)

```
SE Evento.Gratuito (PrecoPadrao == 0):
    → Pular etapa de pagamento
    → Confirmar reserva imediatamente
    → Status ingresso → 2 (Vendido)
    → ValorFinalPago = 0
    → Cupom NÃO é aceito

SE Evento é Pago (PrecoPadrao > 0):
    → Fluxo normal com checkout
    → Cupom pode ser aplicado
    → Status ingresso → 2 após confirmação
```

## 100.7 Respostas

| Código | Caso |
|--------|------|
| `201 Created` | Evento criado com N ingressos gerados |
| `400 Bad Request` | Dados inválidos (data passada, capacidade ≤ 0) |
| `400 Bad Request` | Cupom em evento gratuito: "Cupom não aplicável em evento gratuito" |
