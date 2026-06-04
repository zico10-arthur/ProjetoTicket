# 80 — ST-09: Vendedor como Perfil na Tabela Usuarios

> **Origem:** [`storytelling.md#st-09-vendedor-como-perfil`](../../storytelling.md#st-09-vendedor-como-perfil)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Ter autonomia: arquitetura simplificada elimina a tabela Empresas separada, unificando todos no mesmo login.

---

## 80.1 História

**Como** arquiteto do sistema,
**Quero** que Vendedor seja um perfil na tabela Usuarios (com propriedades específicas),
**Para** simplificar a arquitetura eliminando a tabela Empresas separada.

---

## 80.2 Migração SQL

```sql
-- Adicionar colunas de Vendedor na tabela Usuarios
ALTER TABLE Usuarios ADD
    Cnpj            VARCHAR(14)     NULL,
    NomeFantasia    VARCHAR(200)    NULL,
    Telefone        VARCHAR(20)     NULL,
    LogoUrl         VARCHAR(500)    NULL,
    Descricao       VARCHAR(1000)   NULL,
    Site            VARCHAR(500)    NULL,
    Plano           INT             NULL DEFAULT 0,
    DataCriacao     DATETIME        NOT NULL DEFAULT GETDATE();

-- Migrar dados da tabela Empresas (se existir)
INSERT INTO Usuarios (Cpf, Nome, NomeFantasia, Email, Senha, PerfilId,
                       Cnpj, Telefone, LogoUrl, Descricao, Site, Plano, Ativo, DataCriacao)
SELECT Cnpj, RazaoSocial, NomeFantasia, Email, Senha,
       'B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2',
       Cnpj, Telefone, LogoUrl, Descricao, Site, 0, 1, GETDATE()
FROM Empresas;

-- Remover tabela antiga (após validação)
DROP TABLE Empresas;
```

---

## 80.3 Entidade Usuario (atualizada)

```csharp
public class Usuario
{
    // Comuns a todos os perfis
    public string Cpf { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string Senha { get; private set; }
    public Guid PerfilId { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTime DataCriacao { get; private set; } = DateTime.Now;

    // Específicos do Vendedor (nullable)
    public string? Cnpj { get; private set; }
    public string? NomeFantasia { get; private set; }
    public string? Telefone { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? Descricao { get; private set; }
    public string? Site { get; private set; }
    public PlanoVendedor? Plano { get; private set; }
}
```

---

## 80.4 Colunas (quando preenchidas)

| Coluna | Admin | Comprador | Vendedor |
|--------|-------|-----------|----------|
| Cnpj | `null` | `null` | CNPJ validado |
| NomeFantasia | `null` | `null` | Nome de exibição |
| Telefone | `null` | `null` | Opcional |
| LogoUrl | `null` | `null` | Opcional |
| Descricao | `null` | `null` | Opcional |
| Site | `null` | `null` | Opcional |
| Plano | `null` | `null` | 0 (Gratuito), 1 (Básico) ou 2 (Profissional) |

---

## 80.5 Mudança Estrutural

| Item | Antes | Agora |
|------|-------|-------|
| Tabela do Vendedor | `Empresas` | `Usuarios` |
| Login Vendedor | `POST /api/empresa/login` | `POST /api/usuario/login` |
| Role JWT | `"Empresa"` | `"Vendedor"` |
| FK Eventos | `Eventos.EmpresaId` | `Eventos.VendedorId` → `Usuarios.Cpf` |
| FK Cupons | sem vínculo | `Cupons.VendedorId` → `Usuarios.Cpf` |
| FK Reservas | sem vínculo | `Reservas.VendedorId` → `Usuarios.Cpf` |
