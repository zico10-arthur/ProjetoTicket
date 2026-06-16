# 90 — ST-10: Perfis Simplificados — Admin, Vendedor, Comprador

> **Origem:** [`storytelling.md#st-10-perfis-simplificados`](../../storytelling.md#st-10-perfis-simplificados)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Ter autonomia com controle de acesso simples e bem definido: 3 perfis, regras claras.

---

## 90.1 História

**Como** administrador do sistema,
**Quero** ter apenas três perfis bem definidos (Admin, Vendedor, Comprador),
**Para** simplificar o controle de acesso.

---

## 90.2 Tabela Perfis (SQL Seed)

```sql
-- Garantir que existem exatamente 3 perfis com GUIDs fixos
IF NOT EXISTS (SELECT 1 FROM Perfis WHERE Id = 'A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1')
    INSERT INTO Perfis (Id, Nome) VALUES ('A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1', 'Admin');

IF NOT EXISTS (SELECT 1 FROM Perfis WHERE Id = 'B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2')
    INSERT INTO Perfis (Id, Nome) VALUES ('B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2', 'Vendedor');

IF NOT EXISTS (SELECT 1 FROM Perfis WHERE Id = 'C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3')
    INSERT INTO Perfis (Id, Nome) VALUES ('C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3', 'Comprador');
```

---

## 90.3 GUIDs e Regras de Entrada

| Perfil | GUID | Como entra no sistema | Página de cadastro |
|--------|------|----------------------|---------------------|
| Admin | `A1A1...A1A1` | SQL seed manual | ❌ Não tem |
| Vendedor | `B2B2...B2B2` | Auto cadastro (`POST /api/usuario/cadastrar-vendedor`) | ✅ `VendedorCadastro.razor` |
| Comprador | `C3C3...C3C3` | Cadastro público (`POST /api/usuario/cadastrar`) | ✅ `Cadastro.razor` |

---

## 90.4 Mapeamento de Permissões (Autorização)

| Ação | Admin | Vendedor | Comprador |
|------|-------|----------|-----------|
| Cadastrar-se | ❌ | ✅ (Vendedor) | ✅ (Comprador) |
| Login | ✅ | ✅ | ✅ |
| Ver eventos | ✅ | ✅ | ✅ |
| Criar evento | ❌ | ✅ (próprio) | ❌ |
| Editar/excluir evento | ✅ (qualquer) | ✅ (próprio) | ❌ |
| Fazer reserva | ✅ | ✅ | ✅ |
| Cancelar reserva | ✅ (qualquer) | ✅ (própria/evento) | ✅ (própria) |
| Criar cupom | ✅ (próprio) | ❌ | ❌ |
| Listar vendedores | ✅ | ❌ | ❌ |
| Ativar/desativar vendedor | ✅ | ❌ | ❌ |
| Alterar plano vendedor | ✅ | ❌ | ❌ |

---

## 90.5 Validação no Cadastro

```csharp
// Comprador
public static Usuario CriarComprador(string cpf, string nome, string email, string senha)
{
    return new Usuario
    {
        Cpf = cpf,
        Nome = nome,
        Email = email,
        Senha = BCrypt.HashPassword(senha),
        PerfilId = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3"),
        Ativo = true
    };
}

// Vendedor
public static Usuario CriarVendedor(string cnpj, string nome, string nomeFantasia,
                                     string email, string senha, string? telefone)
{
    return new Usuario
    {
        Cpf = cnpj,  // identificador único
        Nome = nome,
        Email = email,
        Senha = BCrypt.HashPassword(senha),
        PerfilId = Guid.Parse("B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2"),
        Cnpj = cnpj,
        NomeFantasia = nomeFantasia,
        Telefone = telefone,
        Plano = PlanoVendedor.Gratuito,
        Ativo = true
    };
}
```
