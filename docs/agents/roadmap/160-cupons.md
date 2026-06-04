# 160 — Cupons de Desconto

> **Origem:** Requisitos [`requisitos.md`](../../requisitos.md) HU17-HU21, HU27-HU28 e correção do ponto crítico [#2](../../sprints.md)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Emitir ingressos com desconto controlado: Admin cria cupons globais; compradores aplicam em qualquer evento.

---

## 160.1 Regra Principal

```
✅ Admin CRIA e gerencia cupons
✅ Cupom é GLOBAL — válido para qualquer evento, de qualquer vendedor
✅ Comprador APLICA cupom na reserva
❌ Vendedor NÃO gerencia cupons
❌ Cupom NÃO tem vínculo com vendedor específico
```

---

## 160.2 Entidade

```csharp
public class Cupom
{
    public string Codigo { get; private set; }            // Ex: "PROMO10"
    public int PorcentagemDesconto { get; private set; }   // 1 a 100
    public decimal ValorMinimo { get; private set; }       // Valor mínimo da compra
    public DateTime? DataExpiracao { get; private set; }   // null = sem expiração
    public bool Ativo { get; private set; }                // Admin ativa/desativa
}
```

### SQL

```sql
CREATE TABLE Cupons (
    Codigo              VARCHAR(50)     PRIMARY KEY,
    PorcentagemDesconto INT             NOT NULL CHECK (PorcentagemDesconto BETWEEN 1 AND 100),
    ValorMinimo         DECIMAL(10,2)   NOT NULL DEFAULT 0,
    DataExpiracao       DATETIME        NULL,
    Ativo               BIT             NOT NULL DEFAULT 1
);
```

---

## 160.3 Endpoints

### Cadastrar (Admin)

```
POST /api/admin/cupom/cadastrar
Auth: JWT (role=Admin)
```

```json
{
    "codigo": "PROMO10",
    "porcentagemDesconto": 10,
    "valorMinimo": 50.00,
    "dataExpiracao": "2026-12-31T23:59:59"
}
```

### Listar todos os cupons (Admin)

```
GET /api/admin/cupons
Auth: JWT (role=Admin)
```

Retorna todos os cupons (ativos, inativos e expirados).

### Listar cupons disponíveis (Comprador)

```
GET /api/cupom/disponiveis
Auth: JWT
```

Retorna cupons **ativos e não expirados**. Globais — válidos para qualquer evento.

### Ativar/Desativar (Admin)

```
PUT /api/admin/cupom/{codigo}/ativar
Auth: JWT (role=Admin)
```

```json
{ "ativo": false }
```

### Alterar desconto (Admin)

```
PUT /api/admin/cupom/{codigo}/desconto
Auth: JWT (role=Admin)
```

```json
{ "porcentagemDesconto": 15 }
```

### Alterar data de expiração (Admin)

```
PUT /api/admin/cupom/{codigo}/expiracao
Auth: JWT (role=Admin)
```

```json
{ "dataExpiracao": "2026-12-31T23:59:59" }
```

### Excluir (Admin)

```
DELETE /api/admin/cupom/{codigo}
Auth: JWT (role=Admin)
```

---

## 160.4 Validações

| Regra | Erro |
|-------|------|
| `codigo` vazio ou > 50 caracteres | 400 "Código inválido" |
| `codigo` duplicado | 409 "Código já cadastrado" |
| `porcentagemDesconto` < 1 ou > 100 | 400 "Desconto deve ser entre 1 e 100" |
| `valorMinimo` < 0 | 400 "Valor mínimo não pode ser negativo" |
| `dataExpiracao` no passado | 400 "Data de expiração deve ser futura" |

---

## 160.5 Regras de Negócio

### Aplicação do cupom na reserva

```
QUANDO comprador informa cupom na reserva:
    1. Buscar cupom pelo código
       SE não encontrado:
           → 404 "Cupom não encontrado"

    2. Validar cupom:
       SE !Cupom.Ativo:
           → 400 "Cupom desativado"
       SE Cupom.DataExpiracao != null AND Cupom.DataExpiracao < DateTime.Now:
           → 400 "Cupom expirado"
       SE Evento.Gratuito:
           → 400 "Cupom não aplicável em evento gratuito"

    3. Validar valor mínimo:
       ValorTotal = SUM(ItemReserva.PrecoUnitario)
       SE ValorTotal < Cupom.ValorMinimo:
           → 400 "Valor mínimo de R$ {minimo} não atingido"

    4. Aplicar desconto:
       Desconto = ValorTotal * Cupom.PorcentagemDesconto / 100
       ValorFinalPago = MAX(0, ValorTotal - Desconto)
```

### Cálculo

```
ValorTotal = 4 itens × R$50 = R$200
Cupom PROMO10: 10% de desconto, ValorMinimo R$50
Desconto = R$200 × 10 / 100 = R$20
ValorFinalPago = R$200 - R$20 = R$180 ✅

Se ValorTotal = R$40 e Cupom.ValorMinimo = R$50:
→ 400 "Valor mínimo de R$50 não atingido" ❌
```

---

## 160.6 Respostas

| Código | Caso |
|--------|------|
| `201 Created` | Cupom cadastrado |
| `200 OK` | Cupom alterado / listado |
| `400 Bad Request` | Validação (desconto inválido, cupom expirado, valor mínimo não atingido) |
| `404 Not Found` | Cupom não encontrado |
| `409 Conflict` | Código duplicado |
