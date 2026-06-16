# Especificações do Sistema — SoldOut Tickets (SaaS)

## Sumário

1. [Visão Geral do Produto](#1-visão-geral-do-produto)
2. [Modelo de Negócio — Três Perfis de Usuário](#2-modelo-de-negócio--três-perfis-de-usuário)
3. [Como Funciona o Cadastro de Cada Um](#3-como-funciona-o-cadastro-de-cada-um)
4. [Arquitetura](#4-arquitetura)
5. [Entidades do Domínio](#5-entidades-do-domínio)
6. [Tipos de Evento](#6-tipos-de-evento)
7. [Modelos de Precificação](#7-modelos-de-precificação)
8. [Fluxos do Sistema](#8-fluxos-do-sistema)
9. [Requisitos Funcionais](#9-requisitos-funcionais)
10. [Requisitos Não Funcionais](#10-requisitos-não-funcionais)
11. [Regras de Negócio](#11-regras-de-negócio)
12. [Banco de Dados](#12-banco-de-dados)
13. [APIs e Endpoints](#13-apis-e-endpoints)
14. [Plano de Migração](#14-plano-de-migração)
15. [Glossário](#15-glossário)

---

## 1. Visão Geral do Produto

O **SoldOut Tickets** é uma plataforma SaaS voltada para **eventos de pequeno porte** (palestras, workshops, cursos, meetups), conectando **Vendedores** (que criam e vendem eventos) e **Compradores** (o público), permitindo a criação, gestão e venda de reservas:

- **Palestra / Workshop / Curso** — com lotação geral (controle de vagas, sem assento fixo)
- **Teatro / Espetáculo** — com assentos numerados e setores VIP/Geral
- **Show / Concerto** — com áreas ou assentos definidos
- **Evento Corporativo / Meetup** — gratuito ou pago
- **Evento Gratuito** — sem cobrança, apenas controle de presença

```
┌──────────────────────────────────────────────────────────┐
│                    SOLDOUT TICKETS                        │
│                                                          │
│   ┌──────────┐    ┌──────────────┐    ┌──────────────┐  │
│   │  ADMIN    │    │  VENDEDOR    │    │  COMPRADOR   │  │
│   │(sistema)  │    │  (vende)     │    │  (compra)    │  │
│   └─────┬─────┘   └──────┬───────┘   └──────┬───────┘  │
│         │                │                  │           │
│         │ gerencia       │ cria eventos     │ compra     │
│         ▼                ▼                  ▼           │
│   ┌──────────────────────────────────────────────────┐  │
│   │              DADOS DA PLATAFORMA                  │  │
│   │  Eventos | Cupons | Ingressos | Reservas          │  │
│   └──────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

---

## 2. Modelo de Negócio — Três Perfis de Usuário

### 2.1 Quem é Quem

Todos os usuários estão na mesma tabela `Usuarios`, diferenciados pelo `PerfilId`:

| Entidade | O que é | Como entra no sistema |
|----------|---------|----------------------|
| **Admin** | Administrador da plataforma. Gerencia vendedores, visualiza dados globais. | **Cadastrado manualmente no banco** (seed). Não tem cadastro público. |
| **Vendedor** | Pessoa jurídica que quer vender ingressos para seus eventos de pequeno porte. | **Se auto cadastra pelo site** com CNPJ, dados da empresa e senha. |
| **Comprador** | Pessoa física que quer comprar ingressos. | **Se cadastra pelo site** com CPF, nome, email e senha. |

### 2.2 Perfis de Usuário (tabela [`Perfis`](../Domain/Entities/Perfil.cs))

A tabela de usuários possui **três perfis**:

| Perfil | ID (GUID) | Quem usa |
|--------|-----------|----------|
| **Admin** | `A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1` | Administradores da plataforma |
| **Vendedor** | `B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2` | Pessoas jurídicas que criam e vendem eventos (pequeno porte) |
| **Comprador** | `C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3` | Pessoas físicas que compram ingressos |

> ✅ **Vendedor É um perfil de usuário.** Está na tabela `Usuarios`, usa o mesmo endpoint de login (`/api/usuario/login`), e recebe JWT com role=Vendedor.
> ✅ A classe `Usuario` possui propriedades específicas para o perfil Vendedor (CNPJ, NomeFantasia, LogoUrl, Descricao, Site, Plano, Telefone, Ativo), preenchidas apenas quando `PerfilId = B2B2...`.
> ✅ **Auto cadastro**: o Vendedor se cadastra sozinho pelo site — não depende mais do Admin para criar sua conta.

### 2.3 Login — Único Fluxo, Três Roles

Todos fazem login pelo mesmo endpoint. O JWT gerado contém a role correspondente ao perfil:

```
Admin:
  POST /api/usuario/login  →  JWT { role: "Admin", cpf, email }

Vendedor:
  POST /api/usuario/login  →  JWT { role: "Vendedor", cpf, email }

Comprador:
  POST /api/usuario/login  →  JWT { role: "Comprador", cpf, email }
```

---

## 3. Como Funciona o Cadastro de Cada Um

### 3.1 Admin — Cadastro manual no banco

O Admin **não tem página de cadastro**. Inserido diretamente via script SQL:

```sql
INSERT INTO Usuarios (Cpf, Nome, Email, PerfilId, Senha)
VALUES ('00000000000', 'Administrador', 'admin@soldout.com',
        'A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1',
        '$2a$11$...hash...do...bcrypt...');
```

- Apenas **um** Admin (ou poucos, controlados manualmente)
- Senha com hash BCrypt
- Login pelo endpoint `/api/usuario/login`
- JWT com role=Admin

### 3.2 Comprador — Cadastro pelo site

```
Página de Cadastro (Cadastro.razor)
    ↓ Preenche: CPF, Nome, Email, Senha
    ↓
POST /api/usuario/cadastrar
    ↓ Validações: CPF, email, senha, nome
    ↓
Cria Usuario com PerfilId = Comprador (C3C3...)
    ↓
Redireciona para Login
```

- Após login, JWT com role=Comprador
- Acessa Home, compra ingressos, vê Minhas Reservas

### 3.3 Vendedor — Auto Cadastro pelo site (NOVO FLUXO)

> **Mudança importante**: Antes, o Admin cadastrava o Vendedor. Agora, o Vendedor faz **auto cadastro** — ele mesmo se registra no site.

```
Página de Cadastro de Vendedor (VendedorCadastro.razor) — página NOVA
    ↓ Preenche: CNPJ, Nome (Razão Social), Nome Fantasia, Email, Senha, Telefone
    ↓
POST /api/usuario/cadastrar-vendedor
    ↓ Validações:
    │  ├── CNPJ: formato, dígitos verificadores, unicidade
    │  ├── Email: válido, não duplicado entre usuários
    │  ├── Senha: 8+ dígitos, letras, números, caractere especial
    │  └── Nome: não vazio
    ↓
Cria Usuario com PerfilId = Vendedor (B2B2...)
    │  ├── Cpf = CNPJ (campo Cpf armazena o identificador)
    │  ├── Cnpj = CNPJ (campo específico para Vendedor)
    │  ├── NomeFantasia, Telefone preenchidos
    │  └── Plano = Gratuito (padrão)
    ↓
Redireciona para Login (Login.razor)
    ↓
Vendedor faz login → JWT { role: "Vendedor", cpf, email }
    ↓
Acessa Painel do Vendedor:
    ├── Criar Evento (Palestra ou Teatro)
    ├── Meus Eventos (gerenciar, cancelar)
    ├── Gerenciar Cupons
    ├── Relatórios de Vendas
    └── Configurações (logo, descrição, site)
```

### 3.4 Comparativo dos Três Cadastros

| Aspecto | Admin | Comprador | Vendedor |
|---------|-------|-----------|----------|
| **Como cadastra** | Manual (SQL seed) | Pelo site | Pelo site (auto cadastro) |
| **Tabela** | `Usuarios` | `Usuarios` | `Usuarios` |
| **Identificador** | CPF (000...) | CPF | CNPJ |
| **PerfilId** | Admin (A1A1...) | Comprador (C3C3...) | Vendedor (B2B2...) |
| **Role no JWT** | `Admin` | `Comprador` | `Vendedor` |
| **Endpoint login** | `/api/usuario/login` | `/api/usuario/login` | `/api/usuario/login` |
| **Página de cadastro** | ❌ Não tem | ✅ `Cadastro.razor` | ✅ `VendedorCadastro.razor` (NOVA) |
| **Propriedades extras** | Nenhuma | Nenhuma | CNPJ, NomeFantasia, LogoUrl, Descricao, Site, Plano, Telefone |

---

## 4. Arquitetura

### 4.1 Diagrama de Entidades

```
┌──────────────────────────────────────────────────────────────┐
│                       Perfis                                  │
│  A1A1... = Admin | B2B2... = Vendedor | C3C3... = Comprador │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Usuarios (tabela única — todos os perfis)                     │
│ ┌──────────────┐ ┌──────────────────┐ ┌──────────────────┐  │
│ │ Admin        │ │ Vendedor         │ │ Comprador        │  │
│ │ Perfil=Admin │ │ Perfil=Vendedor  │ │ Perfil=Comprador │  │
│ │ Cpf=000...   │ │ Cpf=CNPJ         │ │ Cpf=XXX...       │  │
│ │              │ │ Cnpj, NomeFant,  │ │                  │  │
│ │              │ │ Logo, Plano...   │ │                  │  │
│ └──────────────┘ └────────┬─────────┘ └──────────────────┘  │
│                           │                                    │
│                           ├── Eventos (1:N)                   │
│                           │   ├── Ingressos (1:N) — Teatro    │
│                           │   └── Reservas (1:N)              │
│                           │                                    │
│                           └── Cupons (1:N)                    │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Reservas                                                      │
│ Comprador reserva Ingresso/Evento de um Vendedor             │
└──────────────────────────────────────────────────────────────┘
```

### 4.2 Regras de Isolamento

```
Vendedor X → só vê/edita/exclui seus próprios eventos
Comprador Y → só vê suas próprias reservas
Admin → vê TUDO (visão global da plataforma), gerencia cupons globais
```

Garantido pela coluna `VendedorId` (CPF/CNPJ do Vendedor) nas tabelas de negócio. Cupons são globais (sem `VendedorId`):

```sql
SELECT * FROM Eventos WHERE VendedorId = @vendedorId
SELECT * FROM Cupons  -- global, sem filtro de vendedor
SELECT * FROM Reservas WHERE VendedorId = @vendedorId
```

---

## 5. Entidades do Domínio

### 5.1 [`Usuario`](../Domain/Entities/Usuario.cs) — ATUALIZADO

```csharp
public class Usuario
{
    // Propriedades comuns a todos os perfis
    public string Cpf { get; private set; }              // CPF (PF) ou CNPJ (Vendedor)
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string Senha { get; private set; }            // Hash BCrypt
    public Guid PerfilId { get; private set; }            // Admin, Vendedor ou Comprador
    public bool Ativo { get; private set; } = true;

    // Propriedades específicas do perfil Vendedor (nullable para Admin/Comprador)
    public string? Cnpj { get; private set; }             // CNPJ do Vendedor
    public string? NomeFantasia { get; private set; }     // Nome de exibição
    public string? Telefone { get; private set; }
    public string? LogoUrl { get; private set; }          // Branding próprio
    public string? Descricao { get; private set; }
    public string? Site { get; private set; }
    public PlanoVendedor? Plano { get; private set; }     // Gratuito, Básico ou Profissional
    public DateTime DataCriacao { get; private set; } = DateTime.Now;

    // Factory methods
    public static Usuario CriarComprador(string cpf, string nome, string email, string senha) { ... }
    public static Usuario CriarVendedor(string cnpj, string nome, string nomeFantasia,
                                         string email, string senha, string? telefone) { ... }
}
```

### 5.2 [`Evento`](../Domain/Entities/Evento.cs) — ATUALIZADO

```csharp
public class Evento
{
    public Guid Id { get; private set; }
    public string VendedorId { get; private set; }        // CPF/CNPJ do Vendedor dono
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public string? Local { get; private set; }
    public string? ImagemUrl { get; private set; }
    public TipoEvento Tipo { get; private set; }          // Teatro ou Palestra
    public int CapacidadeTotal { get; private set; }
    public DateTime DataEvento { get; private set; }
    public decimal PrecoPadrao { get; private set; }      // 0 = gratuito
    public bool Gratuito => PrecoPadrao == 0;
    public DateTime DataCriacao { get; private set; }
    public bool Cancelado { get; private set; } = false;  // ← NOVO: evento cancelado

    public List<Ingresso> Ingressos { get; private set; } = new();
}
```

### 5.3 [`TipoEvento`](../Domain/Entities/TipoEvento.cs) — NOVO ENUM

```csharp
public enum TipoEvento
{
    Teatro = 0,     // Assentos numerados com filas e setores
    Palestra = 1,   // Lotação geral (sem assento fixo) — foco principal
}
```

### 5.4 [`Perfil`](../Domain/Entities/Perfil.cs) — MANTIDO (3 perfis)

```csharp
public class Perfil
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; }  // "Admin", "Vendedor", "Comprador"
}
```

Perfis fixos (GUIDs constantes):

| Perfil | GUID |
|--------|------|
| Admin | `A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1` |
| Vendedor | `B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2` |
| Comprador | `C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3` |

### 5.5 [`Ingresso`](../Domain/Entities/Ingresso.cs) — ATUALIZADO

```csharp
public class Ingresso
{
    public Guid Id { get; private set; }
    public Guid EventoId { get; private set; }
    public decimal Preco { get; private set; }
    public string? Posicao { get; private set; }          // Teatro: "Fila X | Assento Y"
    public string? Setor { get; private set; }            // Teatro: "VIP" ou "Geral"
    public int Status { get; private set; }               // 0=Livre, 1=Reservado, 2=Vendido, 3=Reembolsado
    public DateTime? DataBloqueio { get; private set; }
}
```

### 5.6 [`Reserva`](../Domain/Entities/Reserva.cs) — ATUALIZADO

```csharp
public class Reserva
{
    public Guid Id { get; private set; }
    public string VendedorId { get; private set; }       // Vendedor dono do evento
    public string UsuarioCpf { get; private set; }        // Comprador que fez a reserva
    public Guid EventoId { get; private set; }
    public string? CupomUtilizado { get; private set; }
    public decimal ValorFinalPago { get; private set; }
    public bool Reembolsada { get; private set; } = false;
    public DateTime DataReserva { get; private set; } = DateTime.Now;
    public int QuantidadeTotal => Itens?.Count ?? 0;     // Calculado: total de itens

    // Uma reserva pode ter até 4 itens (cada item = 1 CPF)
    public List<ItemReserva> Itens { get; private set; } = new();

    public bool PodeAdicionarMaisItens => Itens.Count < 4;
}
```

### 5.7 [`ItemReserva`](../Domain/Entities/ItemReserva.cs) — NOVA ENTIDADE

```csharp
public class ItemReserva
{
    public Guid Id { get; private set; }
    public Guid ReservaId { get; private set; }          // Reserva pai
    public string CpfParticipante { get; private set; }   // CPF de quem vai ao evento (não precisa estar cadastrado)
    public Guid? IngressoId { get; private set; }         // null para Palestra (Teatro: assento específico)
    public decimal PrecoUnitario { get; private set; }    // Preço pago por este item
    public bool Reembolsado { get; private set; } = false; // Reembolso individual
}
```

> ✅ **Uma reserva pode ter de 1 a 4 itens** — o Comprador compra para si e para outros CPFs.
> ✅ **CPFs dos itens não precisam estar cadastrados** no sistema — são apenas participantes.
> ✅ Cada `ItemReserva` pode ter um `IngressoId` (Teatro) ou ser `null` (Palestra).

### 5.7 [`Cupom`](../Domain/Entities/Cupom.cs) — GLOBAL (Admin)

```csharp
public class Cupom
{
    public string Codigo { get; private set; }
    // Sem VendedorId — cupons são globais, gerenciados pelo Admin
    public int PorcentagemDesconto { get; private set; }
    public decimal ValorMinimo { get; private set; }
    public DateTime? DataExpiracao { get; private set; }
    public bool Ativo { get; private set; }
}
```

### 5.8 [`PlanoVendedor`](../Domain/Entities/PlanoVendedor.cs) — NOVO ENUM

```csharp
public enum PlanoVendedor
{
    Gratuito = 0,     // Limitado: até 3 eventos
    Basico = 1,       // Até 10 eventos/mês
    Profissional = 2, // Eventos ilimitados, relatórios, branding próprio
}
```

---

## 6. Tipos de Evento

O foco da plataforma são **eventos de pequeno porte**, com destaque para o tipo **Palestra**.

### 6.1 Palestra — Lotação Geral (FOCO PRINCIPAL)

- **Não gera ingressos individuais** — apenas controle de vagas
- Comprador seleciona **quantidade de vagas** (1 a N)
- `IngressoId` na reserva = `null`
- `Quantidade` na reserva = número de vagas
- Controle: `VagasDisponiveis = CapacidadeTotal - SUM(Quantidade reservada)`
- Ideal para workshops, cursos, meetups e palestras

### 6.2 Teatro — Assentos Numerados

- Geração automática de `N` ingressos no momento da criação
- 10% VIP (preço × 1.5), 90% Geral (preço padrão)
- 20 assentos por fila
- Posição: `"Fila X | Assento Y"`
- Comprador seleciona assento específico no mapa visual
- 1 reserva = 1 ingresso (`IngressoId` preenchido)

### 6.3 Comparativo

| Característica | Teatro | Palestra |
|---|---|---|
| Assento numerado | ✅ Sim | ❌ Não |
| Mapa visual de assentos | ✅ Sim | ❌ Não |
| Seleção de quantidade | ❌ 1 por vez | ✅ Múltiplas vagas |
| Geração de ingressos | ✅ Automática (N ingressos) | ❌ Controle por vagas |
| IngressoId na reserva | ✅ Preenchido | ❌ Null |
| Preço por unidade | ✅ Sim | ✅ Sim |
| Gratuito | ✅ Sim | ✅ Sim |
| Foco da plataforma | Secundário | ✅ **Principal** |

---

## 7. Modelos de Precificação

### 7.1 Evento Pago (`PrecoPadrao > 0`)

- Fluxo normal: seleção → checkout → pagamento → confirmação
- Ingresso marcado como **Vendido** (Status=2)
- Background Worker libera assentos não pagos após 15 minutos

### 7.2 Evento Gratuito (`PrecoPadrao = 0`)

- Fluxo simplificado: seleção → confirmação direta
- **Pula a tela de pagamento**
- Ingresso direto para **Vendido** (Status=2)
- `ValorFinalPago = 0`
- **Cupons não se aplicam**

```csharp
if (evento.Gratuito && cupom != null)
    throw new CupomNaoAplicavelEmEventoGratuito();
```

---

## 8. Fluxos do Sistema

### 8.1 Fluxo de Cadastro e Login — COMPRADOR

```
[Página Pública] Home.razor
    ↓
[Cadastro.razor] Preenche CPF, Nome, Email, Senha
    ↓
POST /api/usuario/cadastrar → conta criada (Perfil = Comprador)
    ↓
[Login.razor] Email + Senha
    ↓
POST /api/usuario/login → JWT { role: Comprador, cpf, email }
    ↓
Home (lista de eventos disponíveis)
```

### 8.2 Fluxo de Cadastro e Login — VENDEDOR (Auto Cadastro)

```
[Página Pública] Home.razor (botão "Quero Vender")
    ↓
[VendedorCadastro.razor] Preenche CNPJ, Nome, Nome Fantasia, Email, Senha, Telefone
    ↓
POST /api/usuario/cadastrar-vendedor → conta criada (Perfil = Vendedor, Plano = Gratuito)
    ↓
[Login.razor] Email + Senha
    ↓
POST /api/usuario/login → JWT { role: Vendedor, cpf, email }
    ↓
Painel do Vendedor:
    ├── Criar Evento
    ├── Meus Eventos
    ├── Gerenciar Cupons
    ├── Relatórios
    └── Configurações
```

### 8.3 Fluxo de Compra — PALESTRA (Principal)

```
Home → Clica no evento
    ↓
Tela do evento (detalhes, vagas disponíveis)
    ↓ Adiciona itens à reserva (máximo 4):
    │  ├── Item 1: CPF do participante (obrigatório, não precisa estar cadastrado)
    │  ├── Item 2: CPF do participante (opcional)
    │  ├── Item 3: CPF do participante (opcional)
    │  └── Item 4: CPF do participante (opcional)
    ↓
Checkout (aplica cupom opcional)
    ↓
É gratuito? ── Sim ──→ Reserva confirmada (todos os itens)
    ↓ Não
Pagamento → Confirmar → Reserva criada com todos os itens
```

### 8.4 Fluxo de Compra — TEATRO

```
Home → Clica no evento
    ↓
Tela do evento (detalhes + mapa de assentos)
    ↓ Seleciona 1 assento disponível
Checkout (aplica cupom opcional)
    ↓
É gratuito? ── Sim ──→ Reserva confirmada (Status=2)
    ↓ Não
Pagamento → Confirmar → Reserva criada (Status=2)
```

### 8.5 Fluxo de Cancelamento de Evento — REEMBOLSO

```
Vendedor acessa Painel → Meus Eventos
    ↓
Clica em "Cancelar Evento" no evento desejado
    ↓
Sistema verifica:
    ├── Evento é gratuito? → Cancela sem reembolso (não houve cobrança)
    └── Evento é pago?
        ↓
        ├── Nenhum ingresso vendido? → Cancela diretamente
        └── Há ingressos vendidos (Status=2)?
            ↓
            ⚠️ ALERTA: "Este evento possui X ingressos vendidos.
               O cancelamento exigirá reembolso dos compradores.
               Deseja continuar?"
            ↓
            Vendedor confirma cancelamento
            ↓
            Sistema processa reembolso:
            │  ├── Marca Evento.Cancelado = true
            │  ├── Marca Ingressos como Reembolsados (Status=3)
            │  ├── Marca Reservas como Reembolsada = true
            │  └── Notifica compradores sobre reembolso
            ↓
            Evento cancelado. Compradores recebem notificação de reembolso.
```

### 8.6 Fluxo de Cancelamento de Reserva pelo COMPRADOR

```
Comprador acessa Minhas Reservas
    ↓
Clica em "Cancelar Reserva" na reserva desejada
    ↓
Sistema verifica:
    ├── Reserva já está reembolsada? → ❌ "Reserva já foi cancelada."
    ├── Evento já começou (DataEvento <= agora)?
    │   └── ❌ "Não é possível cancelar. O evento já começou."
    └── Evento ainda não começou (DataEvento > agora)?
        ↓
        ⚠️ Confirmação: "Deseja cancelar sua reserva?"
        ↓
        Comprador confirma cancelamento
        ↓
        Sistema processa cancelamento:
        │  ├── Evento é gratuito?
        │  │   └── Cancela reserva (sem reembolso, não houve cobrança)
        │  └── Evento é pago?
        │      ├── Marca Ingresso como Livre (Status=0) — volta à disponibilidade
        │      ├── Marca Reserva como Reembolsada = true
        │      └── Registra valor do reembolso (ValorFinalPago)
        ↓
        Reserva cancelada. Vaga/Assento liberado para outros compradores.
```

### 8.7 Fluxo do ADMIN

```
Login: POST /api/usuario/login (cpf=000..., admin)
    ↓
Dashboard Admin:
├── Vendedores (listar, ativar/desativar, alterar plano)
├── Compradores (listar)
├── Eventos (visão geral de todos)
└── Suporte
```

---

## 9. Requisitos Funcionais

### 9.1 Vendedor

| ID | Requisito | Prioridade |
|---|---|---|
| VEN-01 | Vendedor faz auto cadastro com CNPJ, Nome, Nome Fantasia, Email, Senha, Telefone | Alta |
| VEN-02 | Sistema valida CNPJ (formato + dígitos verificadores + unicidade) | Alta |
| VEN-03 | Sistema valida email (formato + unicidade entre usuários) | Alta |
| VEN-04 | Vendedor faz login → JWT com role=Vendedor | Alta |
| VEN-05 | Vendedor pode editar seus dados (nome, logo, descrição, site) | Média |
| VEN-06 | Admin ativa/desativa vendedor | Alta |
| VEN-07 | Admin altera plano do vendedor | Média |
| VEN-08 | Vendedor inativo não pode fazer login nem criar/editar eventos | Alta |

### 9.2 Evento

| ID | Requisito | Prioridade |
|---|---|---|
| EVT-01 | Vendedor cria evento vinculado a ele (VendedorId) | Alta |
| EVT-02 | Ao criar, escolhe tipo: Teatro (0) ou Palestra (1) | Alta |
| EVT-03 | Teatro: sistema gera N ingressos (10% VIP, 90% Geral) | Alta |
| EVT-04 | Palestra: não gera ingressos, usa controle de vagas | Alta |
| EVT-05 | Evento pode ser gratuito (PrecoPadrao = 0) | Alta |
| EVT-06 | Vendedor adiciona descrição, local, imagem | Média |
| EVT-07 | Vendedor edita apenas seus próprios eventos | Alta |
| EVT-08 | Admin pode ver/excluir qualquer evento | Média |
| EVT-09 | Eventos de vendedores inativos não aparecem na Home | Alta |
| EVT-10 | Vendedor pode cancelar evento | Alta |
| EVT-11 | Cancelamento de evento pago com ingressos vendidos → reembolso obrigatório | Alta |
| EVT-12 | Cancelamento de evento gratuito → sem reembolso (não houve cobrança) | Alta |

### 9.3 Reserva / Compra

| ID | Requisito | Prioridade |
|---|---|---|
| RES-01 | Teatro: cada ItemReserva seleciona 1 assento específico | Alta |
| RES-02 | Palestra: comprador adiciona de 1 a 4 itens (CPFs participantes) | Alta |
| RES-03 | Todos os participantes informam CPF, mas não precisam estar cadastrados no sistema | Alta |
| RES-04 | Sistema valida vagas disponíveis antes de confirmar (soma dos itens) | Alta |
| RES-05 | Reserva limitada a no máximo 4 ItemReserva | Alta |
| RES-06 | Palestra: impede ultrapassar CapacidadeTotal | Alta |
| RES-07 | Gratuito: pula pagamento, confirma imediatamente | Média |
| RES-08 | Cupom não pode ser aplicado em evento gratuito | Média |
| RES-09 | Reserva vinculada ao vendedor do evento | Alta |
| RES-10 | Background Worker libera assentos expirados | Alta |

### 9.4 Reembolso

| ID | Requisito | Prioridade |
|---|---|---|
| REEM-01 | Cancelamento de evento pago com ingressos vendidos exige reembolso | Alta |
| REEM-02 | Sistema alerta o Vendedor sobre a necessidade de reembolso antes de confirmar | Alta |
| REEM-03 | Ingressos reembolsados são marcados como Status=3 (Reembolsado) | Alta |
| REEM-04 | Reservas reembolsadas são marcadas como Reembolsada = true | Alta |
| REEM-05 | Compradores são notificados sobre o reembolso | Média |
| REEM-06 | Evento cancelado não aparece mais na Home | Alta |

### 9.5 Cupom

| ID | Requisito | Prioridade |
|---|---|---|
| CUP-01 | Cupom é global, gerenciado pelo Admin | Alta |
| CUP-02 | Admin gerencia cupons (CRUD completo) | Alta |
| CUP-03 | Cupom válido para qualquer evento pago da plataforma | Alta |
| CUP-04 | Cupom não se aplica a eventos gratuitos | Média |

### 9.6 Comprador

| ID | Requisito | Prioridade |
|---|---|---|
| COM-01 | Comprador se cadastra com CPF, Nome, Email, Senha | Alta |
| COM-02 | Comprador faz login → JWT com role=Comprador | Alta |
| COM-03 | Comprador altera nome, email, senha | Média |
| COM-04 | Comprador vê histórico de reservas | Alta |
| COM-05 | Comprador remove própria conta | Média |

### 9.7 Admin

| ID | Requisito | Prioridade |
|---|---|---|
| ADM-01 | Admin cadastrado manualmente no banco (seed) | Alta |
| ADM-02 | Admin faz login pelo mesmo endpoint de usuário | Alta |
| ADM-03 | Admin lista todos vendedores | Alta |
| ADM-04 | Admin ativa/desativa vendedor | Alta |
| ADM-05 | Admin altera plano do vendedor | Média |
| ADM-06 | Admin lista todos compradores | Média |
| ADM-07 | Admin visualiza eventos de qualquer vendedor | Média |

---

## 10. Requisitos Não Funcionais

| ID | Requisito |
|---|---|
| RNF-01 | Senhas armazenadas com hash (BCrypt) — Admin, Vendedor e Comprador |
| RNF-02 | Autenticação via JWT com roles: Admin, Vendedor, Comprador |
| RNF-03 | Isolamento de dados: toda query filtra por VendedorId |
| RNF-04 | Proteção contra SQL Injection (Dapper parametrizado) |
| RNF-05 | Migrations versionadas (DbUp) |
| RNF-06 | Operações críticas em transações (cancelamento + reembolso é atômico) |
| RNF-07 | Background Worker filtra por vendedor |
| RNF-08 | CNPJ validado com dígitos verificadores (regra de negócio no Domain) |

---

## 11. Regras de Negócio

### 11.1 Regra de Cancelamento e Reembolso

```
SE evento.Gratuito == true:
    → Cancelamento permitido sem restrições
    → Não há reembolso (não houve cobrança)

SE evento.Gratuito == false:
    SE nenhum ingresso vendido (Status != 2):
        → Cancelamento permitido sem reembolso
    SE há ingressos vendidos (Status == 2):
        → ⚠️ Reembolso obrigatório
        → Sistema alerta o Vendedor: "X ingressos vendidos. Reembolso necessário."
        → Vendedor confirma
        → Sistema processa cancelamento + reembolso em transação atômica
```

### 11.2 Regra de Auto Cadastro do Vendedor

```
- Qualquer pessoa jurídica pode se cadastrar como Vendedor
- Não depende mais de aprovação do Admin
- Plano inicial: Gratuito
- Admin pode depois alterar plano ou desativar
```

### 11.3 Regra de Permissão para Fazer Reservas (TODOS os perfis)

```
- Qualquer usuário logado pode fazer reserva, independentemente do perfil:
  ✅ Comprador → pode fazer reserva normalmente
  ✅ Admin → pode fazer reserva usando seu próprio perfil
  ✅ Vendedor → pode fazer reserva usando seu próprio perfil
- Todos os perfis acessam a Home e visualizam eventos disponíveis
- A única restrição é a regra anti-cambista: um CPF não pode ter mais de uma reserva no mesmo evento
```

### 11.4 Regra de Cancelamento de Reserva pelo Usuário (TODOS os perfis)

```
- Qualquer usuário (Comprador, Admin ou Vendedor) pode cancelar sua própria reserva
- Condição: evento ainda não começou (DataEvento > agora)
- Se evento já começou → cancelamento bloqueado
- Se evento é pago → reembolso do valor pago (ValorFinalPago)
- Se evento é gratuito → cancelamento sem reembolso (não houve cobrança)
- Após cancelamento: ingresso volta ao status Livre (Status=0), vaga liberada
- Reserva marcada como Reembolsada = true
```

### 11.5 Regra de Isolamento de Dados

```
- Vendedor X acessa apenas Eventos onde VendedorId = X.Cpf
- Vendedor X acessa apenas Cupons onde VendedorId = X.Cpf
- Comprador Y acessa apenas Reservas onde UsuarioCpf = Y.Cpf
- Admin acessa tudo
```

---

## 12. Banco de Dados

### 12.1 Alterações na Tabela `Usuarios`

```sql
-- Adicionar colunas de Vendedor
ALTER TABLE Usuarios ADD
    Cnpj            VARCHAR(14)         NULL,
    NomeFantasia    VARCHAR(200)        NULL,
    Telefone        VARCHAR(20)         NULL,
    LogoUrl         VARCHAR(500)        NULL,
    Descricao       VARCHAR(1000)       NULL,
    Site            VARCHAR(500)        NULL,
    Plano           INT                 NULL DEFAULT 0,
    Ativo           BIT                 NOT NULL DEFAULT 1,
    DataCriacao     DATETIME            NOT NULL DEFAULT GETDATE();
```

### 12.2 Alterações em Tabelas Existentes

```sql
-- Eventos
ALTER TABLE Eventos ADD
    VendedorId      VARCHAR(14)         NOT NULL,
    Descricao       VARCHAR(2000)       NULL,
    Local           VARCHAR(500)        NULL,
    ImagemUrl       VARCHAR(500)        NULL,
    Tipo            INT                 NOT NULL DEFAULT 1,  -- 1=Palestra (padrão)
    DataCriacao     DATETIME            NOT NULL DEFAULT GETDATE(),
    Cancelado       BIT                 NOT NULL DEFAULT 0;

ALTER TABLE Eventos ADD CONSTRAINT FK_Eventos_Usuarios
    FOREIGN KEY (VendedorId) REFERENCES Usuarios(Cpf);
CREATE INDEX IX_Eventos_VendedorId ON Eventos(VendedorId);

-- Cupons: globais (sem VendedorId), schema atual mantido
-- Tabela Cupons NÃO recebe VendedorId

-- Reservas
ALTER TABLE Reservas ADD
    VendedorId      VARCHAR(14)         NOT NULL,
    Quantidade      INT                 NOT NULL DEFAULT 1,
    Reembolsada     BIT                 NOT NULL DEFAULT 0;
ALTER TABLE Reservas ALTER COLUMN IngressoId UNIQUEIDENTIFIER NULL;
ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Usuarios
    FOREIGN KEY (VendedorId) REFERENCES Usuarios(Cpf);
CREATE INDEX IX_Reservas_VendedorId ON Reservas(VendedorId);

-- Ingressos: adicionar Status=3 para Reembolsado
-- (Status: 0=Livre, 1=Reservado, 2=Vendido, 3=Reembolsado)
```

### 12.3 Relacionamentos

```
Usuarios (Vendedor) (1) ──── (N) Eventos
Usuarios (Vendedor) (1) ──── (N) Cupons
Usuarios (Vendedor) (1) ──── (N) Reservas (via evento)

Eventos (1) ──── (N) Ingressos (apenas Teatro)
Eventos (1) ──── (N) Reservas

Usuarios (Comprador) (1) ──── (N) Reservas
Ingressos (1) ──── (1) Reservas (apenas Teatro)

Perfis (1) ──── (N) Usuarios
```

---

## 13. APIs e Endpoints

### 13.1 Usuário (Admin, Vendedor e Comprador)

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/usuario/cadastrar` | Cadastro de comprador (PF) | ❌ Público |
| `POST` | `/api/usuario/cadastrar-vendedor` | Auto cadastro de vendedor (PJ) | ❌ Público |
| `POST` | `/api/usuario/login` | Login (admin, vendedor ou comprador) | ❌ Público |
| `GET`  | `/api/usuario/{cpf}` | Dados do usuário | ✅ JWT |
| `PUT`  | `/api/usuario/alterar-nome` | Alterar nome | ✅ JWT |
| `PUT`  | `/api/usuario/alterar-email` | Alterar email | ✅ JWT |
| `PUT`  | `/api/usuario/alterar-senha` | Alterar senha | ✅ JWT |
| `PUT`  | `/api/usuario/atualizar-vendedor` | Atualizar dados do vendedor | ✅ JWT Vendedor |
| `DELETE` | `/api/usuario/{cpf}` | Remover conta | ✅ JWT |

### 13.2 Evento

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/evento/criar` | Criar evento (Tipo, gratuito ou pago) | ✅ JWT Vendedor |
| `GET`  | `/api/evento/listar` | Lista pública de eventos (ativos, não cancelados) | ❌ Público |
| `GET`  | `/api/evento/meus` | Eventos do vendedor logado | ✅ JWT Vendedor |
| `GET`  | `/api/evento/{id}` | Detalhes do evento | ❌ Público |
| `PUT`  | `/api/evento/{id}` | Editar evento | ✅ JWT Vendedor |
| `DELETE` | `/api/evento/{id}` | Cancelar evento (com verificação de reembolso) | ✅ JWT Vendedor |

### 13.3 Reserva

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/reserva/criar` | Criar reserva (com Quantidade para Palestra) | ✅ JWT (todos os perfis) |
| `GET`  | `/api/reserva/minhas` | Reservas do usuário logado | ✅ JWT (todos os perfis) |
| `GET`  | `/api/reserva/evento/{eventoId}` | Reservas de um evento | ✅ JWT Vendedor/Admin |
| `DELETE` | `/api/reserva/{id}` | Cancelar reserva (com reembolso se evento não começou) | ✅ JWT (dono da reserva) |

### 13.4 Cupom

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/cupom/cadastrar` | Cadastrar cupom | ✅ JWT Admin |
| `GET`  | `/api/cupom/listar` | Listar todos cupons | ✅ JWT Admin |
| `GET`  | `/api/cupom/validos` | Listar cupons válidos (público) | — |
| `DELETE` | `/api/cupom/{codigo}` | Remover cupom | ✅ JWT Admin |
| `PATCH` | `/api/cupom/{codigo}/status` | Ativar/desativar cupom | ✅ JWT Admin |
| `PATCH` | `/api/cupom/{codigo}/desconto` | Alterar desconto | ✅ JWT Admin |

### 13.5 Admin

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `GET`  | `/api/admin/vendedores` | Listar vendedores | ✅ JWT Admin |
| `GET`  | `/api/admin/vendedor/{cpf}` | Dados do vendedor | ✅ JWT Admin |
| `PUT`  | `/api/admin/vendedor/{cpf}/plano` | Alterar plano | ✅ JWT Admin |
| `PUT`  | `/api/admin/vendedor/{cpf}/ativar` | Ativar/desativar | ✅ JWT Admin |
| `GET`  | `/api/admin/compradores` | Listar compradores | ✅ JWT Admin |

---

## 14. Plano de Migração

### Fase 1 — Fundação (Domain + Infra)

| # | Tarefa | Arquivos |
|---|--------|----------|
| 1 | Atualizar [`Usuario`](../Domain/Entities/Usuario.cs): adicionar Cnpj, NomeFantasia, Telefone, LogoUrl, Descricao, Site, Plano, Ativo, DataCriacao | `Domain/Entities/Usuario.cs` |
| 2 | Criar enum [`TipoEvento`](../Domain/Entities/TipoEvento.cs) | `Domain/Entities/TipoEvento.cs` |
| 3 | Criar enum [`PlanoVendedor`](../Domain/Entities/PlanoVendedor.cs) | `Domain/Entities/PlanoVendedor.cs` |
| 4 | Atualizar [`Evento`](../Domain/Entities/Evento.cs): adicionar VendedorId, Tipo, Descricao, Local, ImagemUrl, Cancelado | `Domain/Entities/Evento.cs` |
| 5 | Atualizar [`Reserva`](../Domain/Entities/Reserva.cs): adicionar VendedorId, Quantidade, Reembolsada; IngressoId nullable | `Domain/Entities/Reserva.cs` |
| 6 | Atualizar [`Cupom`](../Domain/Entities/Cupom.cs): cupons globais (Admin), sem VendedorId | `Domain/Entities/Cupom.cs` |
| 7 | Atualizar [`Ingresso`](../Domain/Entities/Ingresso.cs): Posicao/Setor nullable; Status=3 para Reembolsado | `Domain/Entities/Ingresso.cs` |
| 8 | Atualizar [`Perfil`](../Domain/Entities/Perfil.cs): garantir 3 perfis (Admin, Vendedor, Comprador) | `Domain/Entities/Perfil.cs` |
| 9 | Criar script SQL para alterar tabelas existentes | `Infraestructure/DataBase/Scripts/` |
| 10 | Atualizar `UsuarioRepository`: suportar campos de Vendedor, auto cadastro | `Infraestructure/Repository/UsuarioRepository.cs` |

### Fase 2 — Application

| # | Tarefa | Arquivos |
|---|--------|----------|
| 1 | Atualizar `UsuarioService`: adicionar cadastro de vendedor (auto cadastro), validação de CNPJ | `Application/Service/UsuarioService.cs` |
| 2 | Atualizar `TokenService` para gerar JWT com role=Vendedor | `Application/Service/TokenService.cs` |
| 3 | Atualizar `EventoService`: filtrar por VendedorId, suportar Tipo, implementar cancelamento com reembolso | `Application/Service/EventoService.cs` |
| 4 | Atualizar `ReservaService`: suportar Palestra (Quantidade), validar vagas, marcar reembolso | `Application/Service/ReservaService.cs` |
| 5 | Atualizar `CupomService`: cupons globais, gerenciados pelo Admin | `Application/Service/CupomService.cs` |
| 6 | Criar DTOs de Vendedor (CadastrarVendedorDTO, VendedorResponseDTO) | `Application/DTOs/` |
| 7 | Atualizar `EventoRequestDTO`: adicionar Tipo, Descricao, Local | `Application/DTOs/EventoRequestDTO.cs` |

### Fase 3 — API

| # | Tarefa | Arquivos |
|---|--------|----------|
| 1 | Atualizar `UsuarioController`: adicionar endpoint cadastrar-vendedor e atualizar-vendedor | `Api/Controllers/UsuarioController.cs` |
| 2 | Atualizar `EventoController`: filtrar por vendedor logado; endpoint de cancelamento com reembolso | `Api/Controllers/EventoController.cs` |
| 3 | Atualizar `ReservaController`: Palestra e Quantidade | `Api/Controllers/ReservaController.cs` |
| 4 | Atualizar `Program.cs`: ajustar injeção de dependências | `Api/Program.cs` |

### Fase 4 — Frontend

| # | Tarefa | Arquivos |
|---|--------|----------|
| 1 | Criar página de auto cadastro de vendedor | `Web/Components/Pages/VendedorCadastro.razor` |
| 2 | Atualizar página de login para suportar Vendedor | `Web/Components/Pages/Login.razor` |
| 3 | Criar painel do vendedor (dashboard, meus eventos, cancelar evento) | `Web/Components/Pages/Vendedor/` |
| 4 | Atualizar CriarEvento: Tipo, gratuito/pago, foco em Palestra | Web |
| 5 | Criar tela de detalhes para Palestra (quantidade, sem mapa) | Web |
| 6 | Atualizar ComprarIngressos: sem mapa para Palestra | Web |
| 7 | Criar dashboard Admin com gestão de vendedores | `Web/Components/Pages/Admin/` |
| 8 | Atualizar Home: mostrar vendedor do evento, filtrar não cancelados | Web |
| 9 | Adicionar navegação "Quero Vender" para cadastro de vendedor | Web |
| 10 | Ajustar `CustomAuthStateProvider` para suportar role=Vendedor | `Web/Auth/CustomAuthStateProvider.cs` |

---

## 15. Glossário

| Termo | Definição |
|-------|-----------|
| **Admin** | Administrador da plataforma. Cadastrado manualmente no banco (seed). Gerencia vendedores. |
| **Comprador** | Pessoa física que se cadastra pelo site para comprar ingressos. Perfil Comprador (C3C3...). |
| **Vendedor** | Pessoa jurídica que faz auto cadastro pelo site para criar e vender eventos de pequeno porte. Perfil Vendedor (B2B2...) na tabela `Usuarios`. |
| **Auto Cadastro** | O Vendedor se cadastra sozinho no site — não depende mais do Admin para criar sua conta. |
| **Evento** | Atividade (palestra, teatro, show) criada por um Vendedor. Foco em eventos de pequeno porte. |
| **Palestra** | Tipo de evento principal: lotação geral, sem assentos fixos, controle por vagas. Ideal para workshops e cursos. |
| **Teatro** | Tipo de evento com assentos numerados, filas, setores (VIP/Geral) e mapa visual. |
| **Evento Gratuito** | Evento com PrecoPadrao = 0. Não requer pagamento, confirma imediatamente. |
| **Evento Pago** | Evento com PrecoPadrao > 0. Requer pagamento para confirmação. |
| **Ingresso** | Representação individual de um assento (apenas para Teatro). |
| **Reserva** | Vínculo entre Comprador e um evento/ingresso, com ou sem cupom. |
| **Reembolso** | Processo obrigatório ao cancelar evento pago com ingressos vendidos. Ingressos → Status=3, Reservas → Reembolsada=true. |
| **Cupom** | Desconto percentual global, gerenciado pelo Admin, aplicável em qualquer evento pago da plataforma. |
| **Plano** | Nível de assinatura do Vendedor: Gratuito, Básico ou Profissional. |

---

> **Documento v2.0** — Especificações atualizadas do SoldOut Tickets.
>
> **Três perfis na tabela `Usuarios`:**
> - **Admin** (`A1A1...`) → cadastro manual no banco (seed)
> - **Vendedor** (`B2B2...`) → **auto cadastro** pelo site (CNPJ) — não depende mais do Admin
> - **Comprador** (`C3C3...`) → cadastro pelo site (CPF)
>
> **Foco da plataforma:** eventos de pequeno porte (palestras, workshops, cursos, meetups).
>
> **Regra de reembolso:** cancelar evento pago com ingressos vendidos → reembolso obrigatório dos compradores.