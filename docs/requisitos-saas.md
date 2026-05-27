# Requisitos do Sistema — SoldOut Tickets (SaaS)

> **Documento de Requisitos — Refatoração para SaaS com mudança de perfis.**
>
> **Escopo:** Exclusivamente backend (API .NET 9, Clean Architecture).
> **Nenhuma tarefa de frontend (Blazor/Web) está incluída neste documento.**

---

## Sumário

1. [Visão Geral da Refatoração](#1-visão-geral-da-refatoração)
2. [Modelo de Dados — Entidade por Entidade](#2-modelo-de-dados--entidade-por-entidade)
3. [Enums do Domínio](#3-enums-do-domínio)
4. [DTOs — Alterações Necessárias](#4-dtos--alterações-necessárias)
5. [Regras de Plano (Limites por Assinatura)](#5-regras-de-plano-limites-por-assinatura)
6. [Histórias de Usuário (Backend)](#6-histórias-de-usuário-backend)
7. [Requisitos Funcionais](#7-requisitos-funcionais)
8. [Requisitos Não Funcionais](#8-requisitos-não-funcionais)
9. [Segurança — Correções Obrigatórias](#9-segurança--correções-obrigatórias)
10. [Regras de Negócio e Restrições](#10-regras-de-negócio-e-restrições)
11. [Glossário](#11-glossário)
12. [Apêndice — Mapeamento de Migração](#12-apêndice--mapeamento-de-migração)

---

## 1. Visão Geral da Refatoração

### 1.1 O Que Muda

| Aspecto | Sistema Atual | Sistema Novo (SaaS) |
|---------|---------------|---------------------|
| **Entidades** | Admin, Vendedor, Comprador (todos em `Usuarios`) | Admin, **Empresa** (tabela própria), Comprador |
| **Perfis na tabela `Perfis`** | Admin (A1A1...), Vendedor (B2B2...), Comprador (C3C3...) | Admin (A1A1...), Comprador (C3C3...) |
| **Quem cria eventos** | Vendedor (perfil de usuário) | **Empresa** (entidade separada, tabela `Empresas`) |
| **Login do vendedor** | `POST /api/usuario/login` → JWT role=Vendedor | `POST /api/empresa/login` → JWT role=Empresa |
| **Identificador** | CPF do vendedor | **CNPJ** da empresa |
| **Planos de assinatura** | Não existia | Gratuito, Básico, Profissional |
| **Tipo de evento** | Apenas Teatro (assentos numerados) | Teatro + **Palestra** (lotação geral) |
| **Isolamento de dados** | Nenhum (todos viam tudo) | **Multitenancy** por `EmpresaId` |
| **Hash de senha** | Texto plano (pendente) | **BCrypt** obrigatório |
| **AdminId em body** | Sim (inseguro) | Extraído do **JWT** |
| **Cupons** | Admin gerenciava todos | **Empresa** gerencia seus próprios |

### 1.2 As Três Entidades do SaaS

| Entidade | O que é | Onde fica | Como entra | Role JWT |
|----------|---------|-----------|------------|----------|
| **Admin** | Administrador da plataforma | Tabela `Usuarios` com `PerfilId = A1A1...` | Seed manual (SQL) | `Admin` |
| **Comprador** | Pessoa física que compra ingressos | Tabela `Usuarios` com `PerfilId = C3C3...` | Cadastro pelo site | `Comprador` |
| **Empresa** | Pessoa jurídica que vende eventos | Tabela `Empresas` (NOVA) | Cadastro pelo site | `Empresa` |

### 1.3 Fluxo de Login — Três Caminhos Distintos

```
Admin:
  POST /api/usuario/login (credentials na tabela Usuarios)
  → JWT { role: "Admin", cpf, email }

Comprador:
  POST /api/usuario/login (credentials na tabela Usuarios)
  → JWT { role: "Comprador", cpf, email }

Empresa:
  POST /api/empresa/login (credentials na tabela Empresas — NOVO)
  → JWT { role: "Empresa", empresaId, cnpj, email }
```

### 1.4 Isolamento de Dados (Multitenancy)

Cada empresa enxerga **apenas seus próprios dados**. O isolamento é feito via coluna `EmpresaId`:

```sql
-- Toda query de negócio deve filtrar por EmpresaId
SELECT * FROM Eventos WHERE EmpresaId = @empresaId
SELECT * FROM Cupons  WHERE EmpresaId = @empresaId
SELECT * FROM Reservas WHERE EmpresaId = @empresaId
```

- **Admin** vê **todos os dados** (sem filtro de EmpresaId)
- **Comprador** vê **apenas suas próprias reservas** (filtro por `UsuarioCpf`)

---

## 2. Modelo de Dados — Entidade por Entidade

### 2.1 [`Empresa`](Domain/Entities/Empresa.cs) — NOVA ENTIDADE

**Local:** `Domain/Entities/Empresa.cs` (criar)

```csharp
public class Empresa
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }            // Razão social
    public string NomeFantasia { get; private set; }     // Nome de exibição
    public string Cnpj { get; private set; }             // 14 dígitos, único
    public string Email { get; private set; }            // Login da empresa
    public string Senha { get; private set; }            // Hash BCrypt
    public string? Telefone { get; private set; }
    public string? LogoUrl { get; private set; }         // Branding (plano Profissional)
    public string? Descricao { get; private set; }
    public string? Site { get; private set; }
    public PlanoEmpresa Plano { get; private set; } = PlanoEmpresa.Gratuito;
    public bool Ativo { get; private set; } = true;
    public DateTime DataCriacao { get; private set; }

    // Factory method
    public static Empresa Criar(string nome, string nomeFantasia, string cnpj,
                                 string email, string senha, string? telefone) { ... }

    // Methods
    public void AtivarDesativar(bool ativo) { ... }
    public void AlterarPlano(PlanoEmpresa novoPlano) { ... }
    public void AlterarDados(string? nomeFantasia, string? descricao, string? site, string? logoUrl) { ... }
    public void AlterarSenha(string novaSenha) { ... }
}
```

**Validações de domínio:**
- CNPJ: 14 dígitos, dígitos verificadores válidos (`ValidarCnpj`)
- Email: formato válido (`ValidarEmail`)
- Nome/NomeFantasia: não vazio
- Senha: 8+ caracteres com letras, números e caractere especial (`ValidarSenha`)
- Empresa desativada não pode fazer login

### 2.2 [`Evento`](Domain/Entities/Evento.cs) — ATUALIZADO

**Local:** `Domain/Entities/Evento.cs`

**Estado atual (linha 33):**
```csharp
// Propriedades ATUAIS
public Guid Id { get; private set; }
public string Nome { get; private set; }
public int CapacidadeTotal { get; private set; }
public DateTime DataEvento { get; private set; }
public decimal PrecoPadrao { get; private set; }
public string VendedorCpf { get; private set; } = string.Empty;  // ← será removido
public List<Ingresso> Ingressos { get; private set; } = new();

// Construtor ATUAL (linha 33)
public Evento(string nome, int capacidadetotal, DateTime dataevento, decimal precopadrao, string vendedorCpf)
```

**Estado novo (requerido):**
```csharp
public class Evento
{
    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }          // ← NOVO: FK para Empresas
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }       // ← NOVO
    public string? Local { get; private set; }            // ← NOVO
    public string? ImagemUrl { get; private set; }        // ← NOVO
    public TipoEvento Tipo { get; private set; }          // ← NOVO: Teatro=0, Palestra=1
    public int CapacidadeTotal { get; private set; }
    public DateTime DataEvento { get; private set; }
    public decimal PrecoPadrao { get; private set; }     // 0 = gratuito
    public bool Gratuito => PrecoPadrao == 0;             // ← NOVO: propriedade calculada
    public DateTime DataCriacao { get; private set; }
    public List<Ingresso> Ingressos { get; private set; } = new();

    // NOVO factory method
    public static Evento Criar(string nome, string? descricao, string? local, string? imagemUrl,
                                TipoEvento tipo, int capacidadeTotal, DateTime dataEvento,
                                decimal precoPadrao, Guid empresaId) { ... }

    // Método existente adaptado
    public void GerarLoteIngressos(int quantidadeDesejada) { ... }  // só para Teatro
}
```

**Regras de `GerarLoteIngressos`:**
- Só deve ser chamado quando `Tipo == Teatro`
- 10% VIP (preço × 1.5), 90% Geral (preço padrão)
- 20 assentos por fila
- Posição: `"Fila {letra} | Assento {numero}"`
- Para `Tipo == Palestra`: **não gerar ingressos**

### 2.3 [`Usuario`](Domain/Entities/Usuario.cs) — ATUALIZADO

**Local:** `Domain/Entities/Usuario.cs`

**Estado atual (linha 7):**
```csharp
public string Cpf { get; private set; }
public string Nome { get; private set; }
public string Email { get; private set; }
public string Senha { get; private set; }     // ← texto plano (atualmente)
public Guid PerfilId { get; private set; }    // Admin ou Comprador (Vendedor removido)

// Factory ATUAL (linha 33)
public static Usuario Criar(string cpf, string nome, string email, Guid perfilid, string senha)
```

**Estado novo (requerido):**
```csharp
public class Usuario
{
    public string Cpf { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string Senha { get; private set; }           // ← Hash BCrypt (obrigatório)
    public Guid PerfilId { get; private set; }           // Admin ou Comprador APENAS
    public bool Ativo { get; private set; } = true;      // ← NOVO: permitir desativar conta

    public static Usuario Criar(string cpf, string nome, string email, Guid perfilid, string senha) { ... }
    public void Desativar() { Ativo = false; }
}
```

**Alteração crítica:** O método [`UsuarioService.Login`](Application/Service/UsuarioService.cs:48) atualmente compara senhas em texto plano:
```csharp
// ATUAL (linha 52) — INSEGURO
if (usuario.Senha != dto.Senha)

// NOVO — com BCrypt
if (!BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.Senha))
```

### 2.4 [`Ingresso`](Domain/Entities/Ingresso.cs) — ATUALIZADO

**Local:** `Domain/Entities/Ingresso.cs`

**Estado atual (linha 3):**
```csharp
public class Ingresso
{
    public Guid Id { get; private set; }
    public Guid EventoId { get; private set; }
    public decimal Preco { get; private set; }
    public string Posicao { get; private set; }     // ← non-nullable
    public string Setor { get; private set; }       // ← non-nullable
    public int Status { get; private set; }         // 0=Livre, 1=Reservado, 2=Vendido
    public DateTime? DataBloqueio { get; private set; }
}
```

**Estado novo (requerido):**
```csharp
public class Ingresso
{
    public Guid Id { get; private set; }
    public Guid EventoId { get; private set; }
    public decimal Preco { get; private set; }
    public string? Posicao { get; private set; }    // ← nullable (Palestra não gera ingressos)
    public string? Setor { get; private set; }      // ← nullable
    public int Status { get; private set; }
    public DateTime? DataBloqueio { get; private set; }
}
```

> **Nota:** Para eventos do tipo Palestra, não serão criados registros na tabela `Ingressos`. O controle é feito por `Quantidade` na tabela `Reservas`.

### 2.5 [`Reserva`](Domain/Entities/Reserva.cs) — ATUALIZADO

**Local:** `Domain/Entities/Reserva.cs`

**Estado atual (linha 7):**
```csharp
public class Reserva
{
    public Guid Id { get; private set; }
    public string UsuarioCpf { get; private set; }
    public Guid EventoId { get; private set; }
    public Guid IngressoId { get; private set; }       // ← non-nullable
    public string? CupomUtilizado { get; private set; }
    public decimal ValorFinalPago { get; private set; }

    // Factory ATUAL (linha 26)
    public static Reserva Criar(string usuarioCpf, Evento evento, Ingresso ingresso, Cupom? cupom = null)
}
```

**Estado novo (requerido):**
```csharp
public class Reserva
{
    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }          // ← NOVO: dona do evento
    public string UsuarioCpf { get; private set; }
    public Guid EventoId { get; private set; }
    public Guid? IngressoId { get; private set; }        // ← nullable (Palestra = null)
    public int Quantidade { get; private set; } = 1;     // ← NOVO (Palestra: N vagas)
    public string? CupomUtilizado { get; private set; }
    public decimal ValorFinalPago { get; private set; }
    public DateTime? DataReserva { get; private set; }

    // NOVO factory method (sobrecarga)
    public static Reserva CriarParaTeatro(string usuarioCpf, Evento evento, Ingresso ingresso,
                                           Cupom? cupom = null) { ... }

    public static Reserva CriarParaPalestra(string usuarioCpf, Evento evento, int quantidade,
                                              Cupom? cupom = null) { ... }
}
```

### 2.6 [`Cupom`](Domain/Entities/Cupom.cs) — ATUALIZADO

**Local:** `Domain/Entities/Cupom.cs`

**Estado atual (linha 5):**
```csharp
public class Cupom
{
    public string Codigo { get; private set; }
    public int PorcentagemDesconto { get; private set; }
    public decimal ValorMinimo { get; private set; }
    public DateTime? DataExpiracao { get; private set; }
    public bool Ativo { get; private set; }

    // Factory ATUAL (linha 28)
    public static Cupom Criar(string codigo, int percentDesc, decimal valorMin, DateTime? expiracao)
}
```

**Estado novo (requerido):**
```csharp
public class Cupom
{
    public string Codigo { get; private set; }
    public Guid EmpresaId { get; private set; }          // ← NOVO: dono do cupom
    public int PorcentagemDesconto { get; private set; }
    public decimal ValorMinimo { get; private set; }
    public DateTime? DataExpiracao { get; private set; }
    public bool Ativo { get; private set; }

    // NOVO factory
    public static Cupom Criar(string codigo, Guid empresaId, int percentDesc,
                                decimal valorMin, DateTime? expiracao) { ... }
}
```

### 2.7 [`Perfil`](Domain/Entities/Perfil.cs) — SIMPLIFICADO

**Local:** `Domain/Entities/Perfil.cs`

**Ação:** Remover o perfil Vendedor (B2B2...).

**Perfis restantes:**

| Perfil | ID (GUID) | Funcionalidades |
|--------|-----------|-----------------|
| **Admin** | `A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1` | Gerir empresas, alterar planos, ver tudo |
| **Comprador** | `C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3` | Comprar ingressos, ver reservas |

> **Não é necessário excluir a entidade `Perfil`**, apenas remover o registro do Vendedor no banco via migration SQL e não referenciá-lo mais no código.

### 2.8 [`Pagamento`](Domain/Entities/Pagamento.cs) — MANTIDO

Sem alterações previstas. A entidade permanece como está.

---

## 3. Enums do Domínio

### 3.1 [`TipoEvento`](Domain/Entities/TipoEvento.cs) — NOVO

**Local:** Criar em `Domain/Entities/TipoEvento.cs`

```csharp
public enum TipoEvento
{
    Teatro = 0,     // Assentos numerados com filas e setores (VIP/Geral)
    Palestra = 1,   // Lotação geral, sem assento fixo, controle por quantidade
}
```

### 3.2 [`PlanoEmpresa`](Domain/Entities/PlanoEmpresa.cs) — NOVO

**Local:** Criar em `Domain/Entities/PlanoEmpresa.cs`

```csharp
public enum PlanoEmpresa
{
    Gratuito = 0,       // Até 3 eventos, sem cupons
    Basico = 1,         // Até 10 eventos/mês, cupons ilimitados
    Profissional = 2,   // Eventos ilimitados, cupons ilimitados, relatórios, branding
}
```

---

## 4. DTOs — Alterações Necessárias

### 4.1 [`EventoRequestDTO`](Application/DTOs/EventoRequestDTO.cs) — ATUALIZAR

**Estado atual (linha 5):**
```csharp
public class EventoRequestDTO
{
    public string Nome { get; set; }
    public int CapacidadeTotal { get; set; }
    public DateTime DataEvento { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal PrecoPadrao { get; set; }
    public string VendedorCpf { get; set; } = string.Empty;   // ← será removido
}
```

**Estado novo:**
```csharp
public class EventoRequestDTO
{
    public string Nome { get; set; }
    public string? Descricao { get; set; }                    // ← NOVO
    public string? Local { get; set; }                         // ← NOVO
    public string? ImagemUrl { get; set; }                     // ← NOVO
    public TipoEvento Tipo { get; set; }                      // ← NOVO
    public int CapacidadeTotal { get; set; }
    public DateTime DataEvento { get; set; }
    [Range(0, double.MaxValue)]                               // ← aceita 0 (gratuito)
    public decimal PrecoPadrao { get; set; }
    // Removido: VendedorCpf — empresaId vem do JWT
}
```

### 4.2 [`EventoResponseDTO`](Application/DTOs/EventoResponseDTO.cs) — ATUALIZAR

**Estado atual (linha 3):**
```csharp
public class EventoResponseDTO
{
    public Guid Id { get; set; }
    public string Nome { get; set; }
    public int CapacidadeTotal { get; set; }
    public DateTime DataEvento { get; set; }
    public decimal PrecoPadrao { get; set; }
    public string VendedorCpf { get; set; } = string.Empty;   // ← será removido
}
```

**Estado novo:**
```csharp
public class EventoResponseDTO
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }                       // ← NOVO
    public string NomeEmpresa { get; set; }                   // ← NOVO (nome fantasia)
    public string Nome { get; set; }
    public string? Descricao { get; set; }                    // ← NOVO
    public string? Local { get; set; }                        // ← NOVO
    public string? ImagemUrl { get; set; }                    // ← NOVO
    public TipoEvento Tipo { get; set; }                      // ← NOVO
    public int CapacidadeTotal { get; set; }
    public DateTime DataEvento { get; set; }
    public decimal PrecoPadrao { get; set; }
    public bool Gratuito => PrecoPadrao == 0;                 // ← NOVO
    // Removido: VendedorCpf
}
```

### 4.3 [`ReservarDTO`](Application/DTOs/ReservarDTO.cs) — ATUALIZAR

**Estado atual (linha 3):**
```csharp
public class ReservarDTO
{
    public Guid EventoId { get; set; }
    public Guid IngressoId { get; set; }       // ← obrigatório (non-nullable)
    public string? CupomUtilizado { get; set; }
}
```

**Estado novo:**
```csharp
public class ReservarDTO
{
    public Guid EventoId { get; set; }
    public Guid? IngressoId { get; set; }      // ← nullable (Palestra = null)
    public int Quantidade { get; set; } = 1;   // ← NOVO (Palestra usa isso)
    public string? CupomUtilizado { get; set; }
}
```

### 4.4 [`CadastrarCupomDTO`](Application/DTOs/CadastrarCupomDTO.cs) — ATUALIZAR

**Estado atual (linha 3):**
```csharp
public class CadastrarCupomDTO
{
    public string Codigo { get; set; } = string.Empty;
    public int PorcentagemDesconto { get; set; }
    public decimal ValorMinimo { get; set; }
    public DateTime? DataExpiracao { get; set; }
    // Sem EmpresaId — recebido via AdminLogado (param do service)
}
```

**Estado novo:** Sem alterações nos campos, mas a lógica muda:
- `EmpresaId` deve ser extraído do JWT (não recebido no body)
- `AdminLogado` nos métodos do service deve ser substituído por `empresaId`

### 4.5 DTOs de Empresa — NOVOS

Criar em `Application/DTOs/`:

**`CadastrarEmpresaDTO.cs`:**
```csharp
public class CadastrarEmpresaDTO
{
    public string Nome { get; set; }           // Razão social
    public string NomeFantasia { get; set; }    // Nome de exibição
    public string Cnpj { get; set; }            // 14 dígitos
    public string Email { get; set; }
    public string Senha { get; set; }
    public string? Telefone { get; set; }
}
```

**`LoginEmpresaDTO.cs`:**
```csharp
public class LoginEmpresaDTO
{
    public string Email { get; set; }
    public string Senha { get; set; }
}
```

**`EmpresaResponseDTO.cs`:**
```csharp
public class EmpresaResponseDTO
{
    public Guid Id { get; set; }
    public string Nome { get; set; }
    public string NomeFantasia { get; set; }
    public string Cnpj { get; set; }
    public string Email { get; set; }
    public string? Telefone { get; set; }
    public string? LogoUrl { get; set; }
    public string? Descricao { get; set; }
    public string? Site { get; set; }
    public PlanoEmpresa Plano { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}
```

**`AlterarEmpresaDTO.cs`:**
```csharp
public class AlterarEmpresaDTO
{
    public string? NomeFantasia { get; set; }
    public string? Descricao { get; set; }
    public string? Site { get; set; }
    public string? LogoUrl { get; set; }
    public string? Telefone { get; set; }
}
```

**`AlterarPlanoDTO.cs`:**
```csharp
public class AlterarPlanoDTO
{
    public PlanoEmpresa NovoPlano { get; set; }
}
```

### 4.6 [`CadastrarUsuarioDTO`](Application/DTOs/CadastrarUsuarioDTO.cs) — MANTIDO

Sem alterações. Apenas o `UsuarioService.CadastrarVendedor` será removido (ninguém mais cadastra vendedor).

---

## 5. Regras de Plano (Limites por Assinatura)

### 5.1 Tabela de Planos

| Plano | ID | Limite Eventos | Cupons | Relatórios | Branding | Preço |
|-------|----|----------------|--------|------------|----------|-------|
| **Gratuito** | 0 | Até **3** eventos (total) | ❌ Não permite | ❌ | ❌ | Grátis |
| **Básico** | 1 | Até **10** eventos/mês | ✅ Ilimitados | ❌ | ❌ | R$ 49,90/mês |
| **Profissional** | 2 | **Ilimitados** | ✅ Ilimitados | ✅ | ✅ Logo/Site | R$ 149,90/mês |

### 5.2 Regras de Validação (Backend)

| ID | Regra | Onde validar |
|----|-------|-------------|
| PLN-01 | Ao cadastrar, empresa inicia no plano **Gratuito** | `EmpresaService.Cadastrar` |
| PLN-02 | Admin pode alterar plano a qualquer momento | `AdminService.AlterarPlano` |
| PLN-03 | Plano Gratuito com 3 **eventos futuros ativos** não pode criar o 4º | `EventoService.CriarEventoAsync` — consultar COUNT por EmpresaId |
| PLN-04 | Plano Básico com 10 eventos criados no mês corrente não pode criar mais | `EventoService.CriarEventoAsync` — consultar COUNT por EmpresaId com filtro de mês |
| PLN-05 | Plano Profissional: sem validação de limite | `EventoService.CriarEventoAsync` — pular validação |
| PLN-06 | Plano Gratuito: `CupomService.CadastrarCupom` deve recusar | `CupomService.CadastrarCupom` — verificar plano da empresa |
| PLN-07 | Contagem considera apenas eventos com `DataEvento >= hoje` (não encerrados) | Query no repositório |

### 5.3 Serviço de Validação de Plano (NOVO)

Criar validação centralizada em [`EventoService`](Application/Service/EventoService.cs):
```csharp
private async Task ValidarLimiteDeEventos(Guid empresaId, CancellationToken ct)
{
    var empresa = await _empresaRepository.BuscarPorId(empresaId, ct);
    if (empresa == null) throw new DomainException("Empresa não encontrada");

    switch (empresa.Plano)
    {
        case PlanoEmpresa.Gratuito:
            var count = await _eventoRepository.ContarEventosAtivos(empresaId, ct);
            if (count >= 3) throw new DomainException("Limite de eventos do plano Gratuito atingido.");
            break;

        case PlanoEmpresa.Basico:
            var countMes = await _eventoRepository.ContarEventosNoMes(empresaId, DateTime.Now, ct);
            if (countMes >= 10) throw new DomainException("Limite mensal de eventos do plano Básico atingido.");
            break;

        case PlanoEmpresa.Profissional:
            // Sem limite
            break;
    }
}
```

---

## 6. Histórias de Usuário (Backend)

> Apenas histórias que impactam o backend. Cada história mapeia para um ou mais endpoints/commands.

### 6.1 Comprador

| ID | Título | Quem | Fluxo Backend |
|----|--------|------|---------------|
| HU-C01 | Cadastro de comprador | Visitante | `POST /api/usuario/cadastrar` → [`UsuarioService.CadastrarComprador`](Application/Service/UsuarioService.cs:22) |
| HU-C02 | Login do comprador | Comprador | `POST /api/usuario/login` → [`UsuarioService.Login`](Application/Service/UsuarioService.cs:48) |
| HU-C03 | Alterar nome | Comprador | `PUT /api/usuario/alterarnome/{cpf}` → [`UsuarioService.AlterarNomeAsync`](Application/Service/UsuarioService.cs:97) |
| HU-C04 | Alterar email | Comprador | `PUT /api/usuario/alteraremail/{cpf}` → [`UsuarioService.AlterarEmailAsync`](Application/Service/UsuarioService.cs:85) |
| HU-C05 | Alterar senha | Comprador | `PUT /api/usuario/alterarsenha/{cpf}` → [`UsuarioService.AlterarSenha`](Application/Service/UsuarioService.cs:76) |
| HU-C06 | Remover conta | Comprador | `DELETE /api/usuario/DeletarUsuario/{cpf}` → [`UsuarioService.RemoverUsuario`](Application/Service/UsuarioService.cs:68) |
| HU-C07 | Listar eventos disponíveis | Visitante/Comprador | `GET /api/evento/listar` → [`EventoService.GetAllAsync`](Application/Service/EventoService.cs:22) |
| HU-C08 | Comprar ingresso (Teatro) | Comprador | `POST /api/reserva/FazerReserva` → [`ReservaService.FazerReserva`](Application/Service/ReservaService.cs:32) |
| HU-C09 | Comprar vaga (Palestra) | Comprador | `POST /api/reserva/FazerReserva` (com `IngressoId=null`, `Quantidade=N`) |
| HU-C10 | Aplicar cupom na reserva | Comprador | Incluído no fluxo de `FazerReserva` (campo `CupomUtilizado`) |
| HU-C11 | Ver minhas reservas | Comprador | `GET /api/reserva/ListarPorCpf` → [`ReservaService.ListarMinhasReservas`](Application/Service/ReservaService.cs:69) |
| HU-C12 | Pagar reserva | Comprador | `POST /api/reserva/ConfirmarPagamento/{ingressoId}` |

### 6.2 Empresa

| ID | Título | Quem | Fluxo Backend |
|----|--------|------|---------------|
| HU-E01 | Cadastro de empresa | Visitante | `POST /api/empresa/cadastrar` → **`EmpresaService.Cadastrar`** (NOVO) |
| HU-E02 | Login da empresa | Empresa | `POST /api/empresa/login` → **`EmpresaService.Login`** (NOVO) |
| HU-E03 | Criar evento (Teatro) | Empresa | `POST /api/evento/criar` → [`EventoService.CriarEventoAsync`](Application/Service/EventoService.cs:68) com `Tipo=Teatro` |
| HU-E04 | Criar evento (Palestra) | Empresa | `POST /api/evento/criar` com `Tipo=Palestra` (sem gerar ingressos) |
| HU-E05 | Editar evento | Empresa | `PUT /api/evento/{id}` → [`EventoService.UpdateAsync`](Application/Service/EventoService.cs:84) |
| HU-E06 | Excluir evento | Empresa | `DELETE /api/evento/{id}` → [`EventoService.DeleteAsync`](Application/Service/EventoService.cs:95) |
| HU-E07 | Listar meus eventos | Empresa | `GET /api/evento/meus` → **`EventoService.GetAllByEmpresaAsync`** (NOVO — substitui `GetAllByVendedorAsync`) |
| HU-E08 | Gerenciar cupons | Empresa | `POST /api/cupom/cadastrar` (e demais endpoints) → [`CupomService`](Application/Service/CupomService.cs) (com `EmpresaId` do JWT) |
| HU-E09 | Ver reservas dos eventos | Empresa | `GET /api/reserva/evento/{eventoId}` → (NOVO endpoint) |
| HU-E10 | Editar dados da empresa | Empresa | `PUT /api/empresa/atualizar` → **`EmpresaService.AtualizarDados`** (NOVO) |
| HU-E11 | Ver limite do plano | Empresa | `GET /api/empresa/plano` → **`EmpresaService.ConsultarPlano`** (NOVO) |

### 6.3 Admin

| ID | Título | Quem | Fluxo Backend |
|----|--------|------|---------------|
| HU-A01 | Login do admin | Admin | `POST /api/usuario/login` (cpf=000...) → [`UsuarioService.Login`](Application/Service/UsuarioService.cs:48) |
| HU-A02 | Listar empresas | Admin | `GET /api/admin/empresas` → **`EmpresaService.ListarTodas`** (NOVO) |
| HU-A03 | Ativar/desativar empresa | Admin | `PUT /api/admin/empresa/{id}/ativar` → **`EmpresaService.AtivarDesativar`** (NOVO) |
| HU-A04 | Alterar plano da empresa | Admin | `PUT /api/admin/empresa/{id}/plano` → **`EmpresaService.AlterarPlano`** (NOVO) |
| HU-A05 | Ver dados de uma empresa | Admin | `GET /api/admin/empresa/{id}` → **`EmpresaService.BuscarPorId`** (NOVO) |
| HU-A06 | Listar compradores | Admin | `GET /api/usuario/listar` → [`UsuarioService.ListarUsuariosAsync`](Application/Service/UsuarioService.cs:111) |
| HU-A07 | Visualizar eventos de qualquer empresa | Admin | `GET /api/admin/eventos` → (NOVO endpoint sem filtro de empresa) |
| HU-A08 | Excluir qualquer evento | Admin | `DELETE /api/evento/{id}` (com role=Admin, sem validação de dono) |
| HU-A09 | Visualizar reservas de qualquer empresa | Admin | `GET /api/reserva/Admin/Todas` → (já existe em [`ReservaController`](Api/Controllers/ReservaController.cs:56)) |

### 6.4 Visitante

| ID | Título | Quem | Fluxo Backend |
|----|--------|------|---------------|
| HU-V01 | Ver eventos sem login | Visitante | `GET /api/evento/listar` (público, sem JWT) |
| HU-V02 | Escolher tipo de cadastro | Visitante | (Frontend apenas — sem backend para esta escolha) |

---

## 7. Requisitos Funcionais

### 7.1 Empresa (NOVOS — 100% backend)

| ID | Requisito | Layer | Onde implementar |
|----|-----------|-------|------------------|
| RF-E01 | Criar entidade `Empresa` com propriedades: Id, Nome, NomeFantasia, Cnpj, Email, Senha, Telefone, LogoUrl, Descricao, Site, Plano, Ativo, DataCriacao | Domain | [`Domain/Entities/Empresa.cs`](Domain/Entities/Empresa.cs) (criar) |
| RF-E02 | Criar enum `PlanoEmpresa` com valores Gratuito=0, Basico=1, Profissional=2 | Domain | `Domain/Entities/PlanoEmpresa.cs` (criar) |
| RF-E03 | Validar CNPJ com dígitos verificadores (domínio) | Domain | Método `ValidarCnpj` em `Empresa.cs` |
| RF-E04 | Validar email da empresa (formato + unicidade entre empresas) | Domain + Infra | `Empresa.cs` + `EmpresaRepository.BuscarPorEmail` |
| RF-E05 | Validar senha da empresa (8+ chars, letras+números+especial) | Domain | `Empresa.ValidarSenha` |
| RF-E06 | Armazenar senha com hash BCrypt | Application | `EmpresaService.Cadastrar` — hash antes de persistir |
| RF-E07 | Cadastrar empresa (POST /api/empresa/cadastrar) | API + Application + Infra | `EmpresaController.Cadastrar` → `EmpresaService.Cadastrar` → `EmpresaRepository.Cadastrar` |
| RF-E08 | Login da empresa (POST /api/empresa/login) | API + Application + Infra | `EmpresaController.Login` → `EmpresaService.Login` → verificar BCrypt → gerar JWT |
| RF-E09 | Gerar JWT com role=Empresa (claims: empresaId, cnpj, email) | Application | `TokenService.GerarTokenEmpresa` (NOVO método) |
| RF-E10 | Empresa desativada não pode fazer login | Application | `EmpresaService.Login` — verificar `Ativo == true` |
| RF-E11 | Empresa pode editar dados (nome fantasia, descrição, site, logo) | API + Application | `PUT /api/empresa/atualizar` |
| RF-E12 | Criar interface `IEmpresaRepository` | Domain | `Domain/Interface/IEmpresaRepository.cs` (criar) |
| RF-E13 | Criar interface `IEmpresaService` | Application | `Application/Interfaces/IEmpresaService.cs` (criar) |
| RF-E14 | Criar `EmpresaRepository` com Dapper | Infra | `Infraestructure/Repository/EmpresaRepository.cs` (criar) |
| RF-E15 | Criar script SQL `Script0009_CriarEmpresas.sql` | Infra (DbUp) | `Infraestructure/DataBase/Scripts/Script0009_CriarEmpresas.sql` |
| RF-E16 | Registrar `IEmpresaService` e `IEmpresaRepository` no DI | API | [`Api/Program.cs`](Api/Program.cs) (linha 22-33, adicionar registros) |

### 7.2 Evento (ATUALIZADOS)

| ID | Requisito | Onde | Ação |
|----|-----------|------|------|
| RF-EVT01 | Substituir `VendedorCpf` por `EmpresaId` | [`Evento.cs`](Domain/Entities/Evento.cs:4) | Alterar propriedade e construtor |
| RF-EVT02 | Adicionar propriedade `Tipo` (TipoEvento) | `Evento.cs` | Nova propriedade |
| RF-EVT03 | Adicionar `Descricao`, `Local`, `ImagemUrl` (nullable) | `Evento.cs` | Novas propriedades |
| RF-EVT04 | Adicionar `DataCriacao` | `Evento.cs` | Nova propriedade |
| RF-EVT05 | Adicionar propriedade calculada `Gratuito` | `Evento.cs` | `=> PrecoPadrao == 0` |
| RF-EVT06 | Se Tipo = Teatro, gerar N ingressos (10% VIP, 90% Geral, 20/fila) | `Evento.cs:GerarLoteIngressos` | Lógica existente mantida |
| RF-EVT07 | Se Tipo = Palestra, **não gerar ingressos** | `EventoService.CriarEventoAsync` | Pular `GerarLoteIngressos` |
| RF-EVT08 | Filtrar eventos por EmpresaId (não mais por VendedorCpf) | [`EventoService.cs`](Application/Service/EventoService.cs:9) | Alterar `GetAllByVendedorAsync` para `GetAllByEmpresaAsync` |
| RF-EVT09 | Criar endpoint `GET /api/evento/empresa` para listar eventos da empresa logada | [`EventoController.cs`](Api/Controllers/EventoController.cs:11) | Novo endpoint com `[Authorize(Roles = "Empresa")]` |
| RF-EVT10 | Ao criar evento, extrair `empresaId` do JWT (claim `empresaId`) | `EventoController.Criar` | Não mais receber `VendedorCpf` do body |
| RF-EVT11 | Validar limite do plano antes de criar evento | `EventoService.CriarEventoAsync` | Chamar `ValidarLimiteDeEventos` |
| RF-EVT12 | PrecoPadrao = 0 aceito (gratuito) — remover `[Range(0.01...)]` | [`EventoRequestDTO.cs`](Application/DTOs/EventoRequestDTO.cs) | Alterar Range para `[Range(0, double.MaxValue)]` |
| RF-EVT13 | Eventos de empresas inativas não aparecem na listagem pública | `EventoService.GetAllAsync` | JOIN com `Empresas` e filtrar `Ativo = true` |
| RF-EVT14 | Atualizar `EventoProfile` do AutoMapper | [`Application/Mappings/EventoProfile.cs`](Application/Mappings/EventoProfile.cs) | Mapear novas propriedades |

### 7.3 Reserva (ATUALIZADOS)

| ID | Requisito | Onde | Ação |
|----|-----------|------|------|
| RF-RES01 | Adicionar `EmpresaId` na entidade `Reserva` | [`Reserva.cs`](Domain/Entities/Reserva.cs:7) | Nova propriedade |
| RF-RES02 | Tornar `IngressoId` nullable (Guid?) | `Reserva.cs` | Alterar tipo |
| RF-RES03 | Adicionar `Quantidade` (int, default 1) | `Reserva.cs` | Nova propriedade |
| RF-RES04 | Criar factory `CriarParaTeatro` (IngressoId preenchido, Quantidade=1) | `Reserva.cs` | Novo método |
| RF-RES05 | Criar factory `CriarParaPalestra` (IngressoId=null, Quantidade=N) | `Reserva.cs` | Novo método |
| RF-RES06 | Atualizar [`ReservaService.FazerReserva`](Application/Service/ReservaService.cs:32) para suportar ambos os tipos | [`ReservaService.cs`](Application/Service/ReservaService.cs) | Lógica condicional por tipo |
| RF-RES07 | Validar vagas disponíveis para Palestra antes de reservar | `ReservaService.FazerReserva` | Consultar `SUM(Quantidade)` das reservas existentes |
| RF-RES08 | Validar que cupom pertence à mesma empresa do evento | `ReservaService.FazerReserva` | Consultar `Cupom.EmpresaId == Evento.EmpresaId` |
| RF-RES09 | Atualizar [`ReservarDTO`](Application/DTOs/ReservarDTO.cs): IngressoId nullable, adicionar Quantidade | `ReservarDTO.cs` | Alterar tipos |
| RF-RES10 | Atualizar INSERT no [`ReservaRepository`](Infraestructure/Repository/ReservaRepository.cs:18) com novas colunas | `ReservaRepository.cs` | Adicionar EmpresaId, Quantidade |
| RF-RES11 | Atualizar [`LiberacaoAssentosWorker`](Api/BackgroundTasks/LiberacaoAssentosWorker.cs) para filtrar por empresa | `LiberacaoAssentosWorker.cs` | Adicionar JOIN/WHERE com EmpresaId |
| RF-RES12 | Evento gratuito: pular pagamento, confirmar imediatamente | `ReservaService.FazerReserva` | Se `evento.Gratuito`, marcar como pago direto |

### 7.4 Cupom (ATUALIZADOS)

| ID | Requisito | Onde | Ação |
|----|-----------|------|------|
| RF-CUP01 | Adicionar `EmpresaId` na entidade `Cupom` | [`Cupom.cs`](Domain/Entities/Cupom.cs:5) | Nova propriedade |
| RF-CUP02 | Atualizar factory `Cupom.Criar` para receber `empresaId` | `Cupom.cs` | Novo parâmetro |
| RF-CUP03 | Substituir `AdminLogado` por `empresaId` em todos os métodos do [`CupomService`](Application/Service/CupomService.cs) | `CupomService.cs` | Alterar assinaturas |
| RF-CUP04 | Extrair `empresaId` do JWT nos endpoints de cupom | [`CupomController.cs`](Api/Controllers/CupomController.cs) | Não receber do body |
| RF-CUP05 | Remover endpoint `DebugClaims` do [`CupomController`](Api/Controllers/CupomController.cs:119) | `CupomController.cs` | Remover método |
| RF-CUP06 | Tornar `CadastrarCupom` no [`CupomRepository`](Infraestructure/Repository/CupomRepository.cs:19) assíncrono | `CupomRepository.cs` | `void` → `async Task` |
| RF-CUP07 | Validar plano da empresa antes de criar cupom (Gratuito não pode) | `CupomService.CadastrarCupom` | Verificar `empresa.Plano != Gratuito` |
| RF-CUP08 | Filtrar cupons por `EmpresaId` no SELECT | `CupomRepository.cs` | Adicionar WHERE nas queries |
| RF-CUP09 | Adicionar `EmpresaId` no INSERT de cupons | `CupomRepository.cs` | Nova coluna no SQL |
| RF-CUP10 | Alterar roles nos endpoints de cupom de `Admin` para `Empresa` | `CupomController.cs` | `[Authorize(Roles = "Empresa")]` |

### 7.5 Usuário (ATUALIZADOS)

| ID | Requisito | Onde | Ação |
|----|-----------|------|------|
| RF-USR01 | Adicionar `Ativo` (bool, default true) na entidade `Usuario` | [`Usuario.cs`](Domain/Entities/Usuario.cs:7) | Nova propriedade |
| RF-USR02 | Remover método `CadastrarVendedor` do [`UsuarioService`](Application/Service/UsuarioService.cs:32) | `UsuarioService.cs` | Remover método e interface |
| RF-USR03 | Remover endpoint `CadastrarVendedor/{Id}` do [`UsuarioController`](Api/Controllers/UsuarioController.cs:31) | `UsuarioController.cs` | Remover endpoint |
| RF-USR04 | Implementar BCrypt no hash de senha ao cadastrar | `UsuarioService.CadastrarComprador` | `BCrypt.Net.BCrypt.HashPassword(senha)` |
| RF-USR05 | Implementar BCrypt.Verify no login | `UsuarioService.Login` | Substituir comparação direta |
| RF-USR06 | Adicionar `Ativo` no INSERT do [`UsuarioRepository`](Infraestructure/Repository/UsuarioRepository.cs:21) | `UsuarioRepository.cs` | Adicionar coluna no SQL |
| RF-USR07 | Usuário inativo não pode fazer login | `UsuarioService.Login` | Verificar `usuario.Ativo` |
| RF-USR08 | Remover referência ao perfil Vendedor no [`TokenService`](Application/Service/TokenService.cs:20) | `TokenService.cs` | Remover case "Vendedor" |

### 7.6 Admin (GESTÃO DE PLATAFORMA — NOVOS)

| ID | Requisito | Onde | Ação |
|----|-----------|------|------|
| RF-ADM01 | Criar `AdminController` com endpoints de gestão de empresas | `Api/Controllers/AdminController.cs` (criar) | `[Authorize(Roles = "Admin")]` |
| RF-ADM02 | GET /api/admin/empresas — listar todas empresas | `AdminController` | Retorna `List<EmpresaResponseDTO>` |
| RF-ADM03 | GET /api/admin/empresa/{id} — dados de uma empresa | `AdminController` | Retorna `EmpresaResponseDTO` |
| RF-ADM04 | PUT /api/admin/empresa/{id}/plano — alterar plano | `AdminController` | Altera `PlanoEmpresa` |
| RF-ADM05 | PUT /api/admin/empresa/{id}/ativar — ativar/desativar | `AdminController` | Alterna `Ativo` |
| RF-ADM06 | GET /api/admin/usuarios — listar compradores | `AdminController` | Reutiliza `UsuarioService.ListarUsuariosAsync` |

### 7.7 Ingresso (MANTIDO + PEQUENAS ALTERAÇÕES)

| ID | Requisito | Onde | Ação |
|----|-----------|------|------|
| RF-ING01 | `Posicao` e `Setor` nullable (para compatibilidade com Palestra — não gera ingressos) | [`Ingresso.cs`](Domain/Entities/Ingresso.cs) | Alterar para `string?` |
| RF-ING02 | Ao listar ingressos de evento, ignorar se Tipo = Palestra | [`IngressoService.ListarIngressosDoEventoAsync`](Application/Service/IngressoService.cs:24) | Retornar lista vazia para Palestra |

### 7.8 Background Worker (ATUALIZADO)

| ID | Requisito | Onde | Ação |
|----|-----------|------|------|
| RF-BW01 | [`LiberacaoAssentosWorker`](Api/BackgroundTasks/LiberacaoAssentosWorker.cs) deve considerar multitenancy | `LiberacaoAssentosWorker.cs` | Adicionar filtro por `EmpresaId` no SQL de liberação |
| RF-BW02 | Worker também deve limpar reservas não pagas de Palestra (sem IngressoId) | `LiberacaoAssentosWorker.cs` | Adicionar `DeletarReservasNaoPagasExpiradas` para Palestra também |

---

## 8. Requisitos Não Funcionais

| ID | Requisito | Categoria | Onde |
|----|-----------|-----------|------|
| RNF-01 | Senhas armazenadas com hash BCrypt (Admin, Comprador e Empresa) | Segurança | `UsuarioService.CadastrarComprador`, `EmpresaService.Cadastrar` |
| RNF-02 | Autenticação via JWT com roles: Admin, Comprador, Empresa | Segurança | `TokenService.GerarToken`, `TokenService.GerarTokenEmpresa` |
| RNF-03 | Isolamento de dados: toda query de negócio filtra por `EmpresaId` | Segurança / Multitenancy | `EventoRepository`, `CupomRepository`, `ReservaRepository` |
| RNF-04 | Proteção contra SQL Injection (Dapper parametrizado) — mantido | Segurança | Todos os repositórios |
| RNF-05 | Migrations versionadas com DbUp — mantido | Infraestrutura | `DatabaseMigration.cs` |
| RNF-06 | Operações críticas em transações — mantido | Integridade | `EventoRepository.CriarEventoCompletoAsync` |
| RNF-07 | Background Worker filtra por empresa | Performance | `LiberacaoAssentosWorker.cs` |
| RNF-08 | CNPJ validado com dígitos verificadores (regra de domínio) | Validação | `Empresa.cs` (método estático `ValidarCnpj`) |
| RNF-09 | API deve extrair `empresaId` do JWT (nunca receber do cliente) | Segurança | `EventoController`, `CupomController` |
| RNF-10 | AdminId deve ser extraído do JWT, nunca recebido no body da requisição | Segurança | `CupomController`, `UsuarioController.CadastrarVendedor` (removido) |
| RNF-11 | Endpoints públicos de consulta devem ser mínimos e controlados | Segurança | Apenas GET /api/evento/listar, GET /api/evento/{id} |
| RNF-12 | Eventos de empresas inativas não devem ser retornados em listagens públicas | Negócio | `EventoService.GetAllAsync` com JOIN |
| RNF-13 | Remover endpoint `ListarUsuarioEspecifico/{cpf}` sem `[Authorize]` | Segurança | [`UsuarioController.cs:52`](Api/Controllers/UsuarioController.cs:52) — adicionar `[Authorize(Roles = "Admin")]` |
| RNF-14 | Remover endpoint `DebugClaims` | Segurança | [`CupomController.cs:119`](Api/Controllers/CupomController.cs:119) — remover |
| RNF-15 | BCrypt.Net nuget package adicionado ao projeto | Dependência | `Application/Application.csproj` |
| RNF-16 | A contagem de eventos para limite de plano considera apenas eventos futuros | Negócio | Query no `EventoRepository` |

---

## 9. Segurança — Correções Obrigatórias

### 9.1 Problemas de Segurança Identificados no Código Atual

| # | Problema | Local | Risco | Correção |
|---|----------|-------|-------|----------|
| S-01 | Senhas armazenadas em texto plano | [`UsuarioService.cs:52`](Application/Service/UsuarioService.cs:48) — `if (usuario.Senha != dto.Senha)` | Exposição de credenciais | BCrypt hash + verify |
| S-02 | `AdminLogado` recebido do body/rota (não do JWT) | [`CupomService.cs`](Application/Service/CupomService.cs:20) — parâmetro `Guid AdminLogado` | Qualquer um pode enviar qualquer ID | Extrair `empresaId` do JWT |
| S-03 | `VendedorCpf` recebido do body | [`EventoRequestDTO.cs:18`](Application/DTOs/EventoRequestDTO.cs) — `VendedorCpf` | Falsificação de autoria | Extrair `empresaId` do JWT |
| S-04 | Endpoint público de CPF sem auth | [`UsuarioController.cs:52`](Api/Controllers/UsuarioController.cs:52) — `[HttpGet("ListarUsuarioEspecifico/{cpf}")]` sem `[Authorize]` | Vazamento de dados | Adicionar `[Authorize(Roles = "Admin")]` |
| S-05 | Endpoint `DebugClaims` expõe claims do JWT | [`CupomController.cs:119`](Api/Controllers/CupomController.cs:119) | Vazamento de info de autenticação | Remover endpoint |
| S-06 | `CadastrarVendedor` ainda existe | [`UsuarioController.cs:31`](Api/Controllers/UsuarioController.cs:31) | Perfil Vendedor não deve mais existir | Remover endpoint e service |
| S-07 | Nenhuma validação de empresa inativa | `EmpresaService.Login` (NOVO) — precisa existir | Empresa desativada pode operar | Validar `Ativo == true` no login |

### 9.2 Novas Validações de Segurança

| # | Validação | Local |
|---|-----------|-------|
| S-08 | CNPJ válido com dígitos verificadores | `Empresa.ValidarCnpj` |
| S-09 | Empresa desativada: login bloqueado | `EmpresaService.Login` |
| S-10 | Comprador/Admin inativo: login bloqueado | `UsuarioService.Login` |
| S-11 | Cupom só pode ser gerenciado pela empresa dona | `CupomService` — filtrar por `empresaId` do JWT |
| S-12 | Evento só pode ser editado/excluído pela empresa dona | `EventoService.UpdateAsync/DeleteAsync` — comparar `empresaId` |
| S-13 | Reserva só pode usar cupom da mesma empresa do evento | `ReservaService.FazerReserva` — validar `Cupom.EmpresaId == Evento.EmpresaId` |

---

## 10. Regras de Negócio e Restrições

### 10.1 Autenticação e Autorização

| ID | Regra |
|----|-------|
| REG-01 | Empresa desativada (`Ativo = false`) não pode fazer login |
| REG-02 | Empresa desativada não pode criar/editar eventos |
| REG-03 | Eventos de empresa desativada não aparecem na listagem pública |
| REG-04 | Comprador com conta removida não pode fazer login |
| REG-05 | Apenas Admin pode ver dados de todas as empresas |
| REG-06 | Apenas Admin pode alterar plano ou ativar/desativar empresa |
| REG-07 | Usuário com perfil Vendedor (B2B2...) não existe mais — deve ser migrado |

### 10.2 Eventos

| ID | Regra |
|----|-------|
| REG-08 | Data do evento deve ser futura (ou no mínimo amanhã) |
| REG-09 | Capacidade deve ser > 0 |
| REG-10 | Teatro: assentos gerados na criação, não podem ser adicionados depois |
| REG-11 | Palestra: não gera ingressos individuais, apenas controle de vagas |
| REG-12 | PrecoPadrao = 0 = evento gratuito |
| REG-13 | Evento gratuito não aceita cupons |
| REG-14 | Evento pertence a uma empresa (EmpresaId obrigatório) |
| REG-15 | Evento de empresa desativada deve ser ocultado de listagens públicas |

### 10.3 Reservas

| ID | Regra |
|----|-------|
| REG-16 | Mesmo CPF não pode ter duas reservas no mesmo evento |
| REG-17 | Palestra: vagas reservadas não podem exceder CapacidadeTotal |
| REG-18 | Teatro: 1 reserva = exatamente 1 ingresso |
| REG-19 | Reserva não paga expira em 15 minutos (`DataBloqueio`) |
| REG-20 | Cupom válido apenas para eventos da mesma empresa que criou o cupom |
| REG-21 | Evento gratuito: pula pagamento, confirma imediatamente |

### 10.4 Cupons

| ID | Regra |
|----|-------|
| REG-22 | Cupom só pode ser usado em eventos pagos (PrecoPadrao > 0) |
| REG-23 | Desconto não pode gerar valor final negativo |
| REG-24 | Cupom expirado não pode ser aplicado |
| REG-25 | Cupom inativo não pode ser aplicado |
| REG-26 | Valor mínimo da compra deve ser respeitado |
| REG-27 | Cupom pertence a uma empresa (EmpresaId obrigatório) |

### 10.5 Plano

| ID | Regra |
|----|-------|
| REG-28 | Empresa inicia no plano Gratuito |
| REG-29 | Plano Gratuito: máximo 3 eventos futuros |
| REG-30 | Plano Básico: máximo 10 eventos/mês |
| REG-31 | Plano Profissional: eventos ilimitados |
| REG-32 | Plano Gratuito: sem cupons |
| REG-33 | Apenas Admin pode alterar plano |

### 10.6 Dados e Migração

| ID | Regra |
|----|-------|
| REG-34 | Perfil Vendedor (B2B2...) não existe mais |
| REG-35 | Usuários com PerfilId = Vendedor devem ser migrados para tabela `Empresas` |
| REG-36 | Admin não pode ser removido pela API (apenas seed manual) |

---

## 11. Glossário

| Termo | Definição |
|-------|-----------|
| **Admin** | Administrador da plataforma SaaS. Cadastrado manualmente no banco (seed). Gerencia empresas. |
| **Comprador** | Pessoa física que se cadastra pelo site para comprar ingressos. Perfil na tabela `Usuarios`. |
| **Empresa** | Pessoa jurídica que se cadastra pelo site para criar e vender eventos. Tabela própria `Empresas`. |
| **Evento** | Atividade (Teatro ou Palestra) criada por uma Empresa. |
| **Teatro** | Tipo de evento com assentos numerados, filas, setores (VIP/Geral) e mapa visual. |
| **Palestra** | Tipo de evento com lotação geral, sem assentos fixos. Controle por quantidade de vagas. |
| **Evento Gratuito** | Evento com `PrecoPadrao = 0`. Confirmação imediata, sem pagamento. |
| **Ingresso** | Representação individual de um assento numerado (apenas para Teatro). |
| **Reserva** | Vínculo entre um Comprador e um evento, com 1 ingresso (Teatro) ou N vagas (Palestra). |
| **Cupom** | Desconto percentual vinculado a uma Empresa. Válido apenas em eventos pagos da mesma empresa. |
| **Plano** | Nível de assinatura da Empresa (Gratuito/Básico/Profissional). Define limites de uso. |
| **Multitenancy** | Isolamento de dados por empresa via coluna `EmpresaId` em todas as tabelas de negócio. |
| **BCrypt** | Algoritmo de hash para senhas. Substitui a comparação em texto plano atual. |
| **DbUp** | Biblioteca de migração de banco de dados versionada. Mantida do sistema atual. |

---

## 12. Apêndice — Mapeamento de Migração

### 12.1 Comparativo Antigo → Novo

| Item Antigo | Item Novo | Ação |
|-------------|-----------|------|
| `Usuario.PerfilId = Vendedor (B2B2...)` | ❌ Removido | Migrar para `Empresas` |
| `Evento.VendedorCpf` | `Evento.EmpresaId` | Alterar coluna + popular com FK |
| `Cupom` sem dono | `Cupom.EmpresaId` | Adicionar coluna |
| `Reserva.IngressoId` non-nullable | `Reserva.IngressoId` nullable | ALTER COLUMN |
| `Reserva` sem quantidade | `Reserva.Quantidade` | Adicionar coluna |
| `Reserva` sem empresa | `Reserva.EmpresaId` | Adicionar coluna |
| `Usuario` sem ativo | `Usuario.Ativo` | Adicionar coluna |
| `UsuarioService.CadastrarVendedor` | ❌ Removido | Remover método |
| `CupomService` com `AdminLogado` | `CupomService` com `empresaId` do JWT | Refatorar |
| `EventoController` role=Vendedor | `EventoController` role=Empresa | Alterar `[Authorize]` |
| `CupomController` role=Admin | `CupomController` role=Empresa | Alterar `[Authorize]` |
| Senha em texto plano | Senha com BCrypt | Alterar hash + verify |
| `TokenService` com role=Vendedor | `TokenService` sem Vendedor | Remover + add Empresa |

### 12.2 Scripts de Banco Necessários

| Script | Descrição |
|--------|-----------|
| `Script0009_CriarEmpresas.sql` | CREATE TABLE Empresas |
| `Script0010_AlterarEventos.sql` | ADD EmpresaId, Tipo, Descricao, Local, ImagemUrl, DataCriacao; DROP VendedorCpf |
| `Script0011_AlterarCupons.sql` | ADD EmpresaId |
| `Script0012_AlterarReservas.sql` | ADD EmpresaId, Quantidade; ALTER IngressoId para nullable |
| `Script0013_AlterarUsuarios.sql` | ADD Ativo |
| `Script0014_RemoverPerfilVendedor.sql` | DELETE FROM Perfis WHERE Nome = 'Vendedor' |
| `Script0015_MigrarVendedores.sql` | Migrar dados de Vendedor para Empresa (se houver) |

---

> **Documento v2.0 — Requisitos de backend para refatoração SaaS do SoldOut Tickets.**
>
> **Este documento contém APENAS requisitos de backend. Nenhuma tarefa de frontend está incluída.**
> **Para o plano de sprints, consulte [`docs/plano-sprints.md`](docs/plano-sprints.md).**
