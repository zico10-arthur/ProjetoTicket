# Especificações do Sistema — SoldOut Tickets (SaaS)

## Sumário

1. [Visão Geral do Produto](#1-visão-geral-do-produto)
2. [Modelo de Negócio — Três Entidades](#2-modelo-de-negócio--três-entidades)
3. [Como Funciona o Cadastro de Cada Um](#3-como-funciona-o-cadastro-de-cada-um)
4. [Arquitetura](#4-arquitetura)
5. [Entidades do Domínio](#5-entidades-do-domínio)
6. [Tipos de Evento](#6-tipos-de-evento)
7. [Modelos de Precificação](#7-modelos-de-precificação)
8. [Fluxos do Sistema](#8-fluxos-do-sistema)
9. [Requisitos Funcionais](#9-requisitos-funcionais)
10. [Requisitos Não Funcionais](#10-requisitos-não-funcionais)
11. [Banco de Dados](#11-banco-de-dados)
12. [APIs e Endpoints](#12-apis-e-endpoints)
13. [Plano de Migração](#13-plano-de-migração)
14. [Glossário](#14-glossário)

---

## 1. Visão Geral do Produto

O **SoldOut Tickets** é uma plataforma SaaS que conecta **Empresas** (que vendem eventos) e **Compradores** (o público), permitindo a criação, gestão e venda de reservas para eventos de qualquer natureza:

- **Teatro / Espetáculo** — com assentos numerados e setores VIP/Geral
- **Palestra / Workshop / Curso** — com lotação geral (controle de vagas, sem assento fixo)
- **Show / Concerto** — com áreas ou assentos definidos
- **Evento Corporativo / Meetup** — gratuito ou pago
- **Evento Gratuito** — sem cobrança, apenas controle de presença

```
┌──────────────────────────────────────────────────────────┐
│                    SOLDOUT TICKETS                        │
│                                                          │
│   ┌──────────┐    ┌──────────────┐    ┌──────────────┐  │
│   │  ADMIN    │    │   EMPRESA    │    │  COMPRADOR   │  │
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

## 2. Modelo de Negócio — Três Entidades

### 2.1 Quem é Quem

| Entidade | O que é | Como entra no sistema |
|----------|---------|----------------------|
| **Admin** | Administrador da plataforma. Gerencia empresas, visualiza dados. | **Cadastrado manualmente no banco** (seed). Não tem cadastro público. |
| **Empresa** | Pessoa jurídica que quer vender ingressos para seus eventos. | **Se cadastra pelo site** com CNPJ, dados da empresa e senha. |
| **Comprador** | Pessoa física que quer comprar ingressos. | **Se cadastra pelo site** com CPF, nome, email e senha. (Como já funciona hoje) |

### 2.2 Perfis de Usuário (tabela [`Perfis`](../Domain/Entities/Perfil.cs))

Apenas **dois perfis** na tabela de usuários:

| Perfil | ID (GUID) | Quem usa |
|--------|-----------|----------|
| **Admin** | `A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1` | Usuários administradores da plataforma |
| **Comprador** | `C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3` | Usuários que compram ingressos |

> ✅ **Empresa NÃO é um perfil de usuário.** Empresa é uma entidade separada, com sua própria tabela (`Empresas`), seu próprio login e seu próprio token JWT.
> ✅ **Perfil Vendedor (B2B2...)** será removido — substituído pela entidade Empresa.

### 2.3 Login — Três Fluxos Diferentes

Cada entidade faz login de forma diferente, e cada uma recebe um JWT com claims diferentes:

```
Admin:
  POST /api/usuario/login  →  JWT { role: "Admin", cpf, email }

Comprador:
  POST /api/usuario/login  →  JWT { role: "Comprador", cpf, email }

Empresa:
  POST /api/empresa/login  →  JWT { role: "Empresa", empresaId, cnpj, email }
```

---

## 3. Como Funciona o Cadastro de Cada Um

### 3.1 Admin — Cadastro manual no banco

O Admin **não tem página de cadastro**. O único Admin é inserido diretamente via script SQL:

```sql
-- Script de seed (executado uma vez)
INSERT INTO Usuarios (Cpf, Nome, Email, PerfilId, Senha)
VALUES ('00000000000', 'Administrador', 'admin@soldout.com',
        'A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1',
        '$2a$11$...hash...do...bcrypt...');
```

- Apenas **um** Admin (ou poucos, controlados manualmente)
- Senha já vem com hash BCrypt
- Admin faz login pelo mesmo endpoint de usuário (`/api/usuario/login`)
- O sistema identifica pelo PerfilId que é Admin e gera JWT com role=Admin

### 3.2 Comprador — Cadastro pelo site (fluxo atual mantido)

```
Página de Cadastro (Cadastro.razor)
    ↓ Preenche: CPF, Nome, Email, Senha
    ↓
POST /api/usuario/cadastrar
    ↓ Validações: CPF, email, senha, nome
    ↓
Cria Usuario com PerfilId = Comprador
    ↓
Redireciona para Login
```

- Fluxo idêntico ao que já existe hoje
- Após login, recebe JWT com role=Comprador
- Acessa Home, compra ingressos, vê Minhas Reservas

### 3.3 Empresa — Cadastro pelo site (novo fluxo)

```
Página de Cadastro Empresarial (EmpresaCadastro.razor) — página NOVA
    ↓ Preenche: CNPJ, Nome, Nome Fantasia, Email, Senha, Telefone
    ↓
POST /api/empresa/cadastrar
    ↓ Validações:
    │  ├── CNPJ: formato, dígitos verificadores, unicidade
    │  ├── Email: válido, não duplicado
    │  ├── Senha: mesmas regras (8+ dígitos, letras, números, especial)
    │  └── Nome: não vazio
    ↓
Cria Empresa com Plano = Gratuito (padrão)
    ↓
Redireciona para Login Empresarial (EmpresaLogin.razor)
    ↓
Empresa faz login → recebe JWT { role: "Empresa", empresaId, cnpj }
    ↓
Acessa Painel da Empresa:
    ├── Criar Evento
    ├── Meus Eventos
    ├── Gerenciar Cupons
    ├── Relatórios
    └── Configurações (logo, descrição, site)
```

### 3.4 Comparativo dos Três Cadastros

| Aspecto | Admin | Comprador | Empresa |
|---------|-------|-----------|---------|
| **Como cadastra** | Manual (SQL seed) | Pelo site | Pelo site |
| **Tabela** | `Usuarios` | `Usuarios` | `Empresas` |
| **Identificador** | CPF (000...) | CPF | CNPJ |
| **PerfilId** | Admin (A1A1...) | Comprador (C3C3...) | N/A (não é usuario) |
| **Role no JWT** | `Admin` | `Comprador` | `Empresa` |
| **Endpoint login** | `/api/usuario/login` | `/api/usuario/login` | `/api/empresa/login` |
| **Página de cadastro** | ❌ Não tem | ✅ `Cadastro.razor` | ✅ `EmpresaCadastro.razor` (NOVA) |

---

## 4. Arquitetura

### 4.1 Diagrama de Entidades

```
┌──────────────────────────────────────────────────────────────┐
│                       Perfis                                  │
│  A1A1... = Admin   |   C3C3... = Comprador                    │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Usuarios                                                      │
│ ┌──────────────┐  ┌──────────────────┐                       │
│ │ Admin        │  │ Comprador        │                       │
│ │ Perfil=Admin │  │ Perfil=Comprador │                       │
│ │ Cpf=000...   │  │ Cpf=XXX...       │                       │
│ └──────────────┘  └──────────────────┘                       │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Empresas                                                      │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ Id | Nome | CNPJ | Email | Senha | Logo | Plano | Ativo  │ │
│ └──────────────────────────────────────────────────────────┘ │
│   │                                                          │
│   ├── Eventos (1:N)                                          │
│   │   ├── Ingressos (1:N) — apenas Teatro                    │
│   │   └── Reservas (1:N)                                     │
│   │                                                          │
│   └── Cupons (1:N)                                           │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ Reservas                                                      │
│ Comprador (Usuario) reserva Ingresso/Evento de uma Empresa   │
└──────────────────────────────────────────────────────────────┘
```

### 4.2 Regras de Isolamento

Toda operação no sistema respeita o dono dos dados:

```
Empresa X → só vê/edit EXCLUI seus próprios eventos e cupons
Comprador Y → só vê suas próprias reservas
Admin → vê TUDO (visão global da plataforma)
```

No banco de dados, isso é garantido pela coluna `EmpresaId` em todas as tabelas de negócio:

```sql
SELECT * FROM Eventos WHERE EmpresaId = @empresaId
SELECT * FROM Cupons WHERE EmpresaId = @empresaId
SELECT * FROM Reservas WHERE EmpresaId = @empresaId
```

---

## 5. Entidades do Domínio

### 5.1 [`Empresa`](../Domain/Entities/Empresa.cs) — NOVA ENTIDADE

```csharp
public class Empresa
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }           // Razão social
    public string NomeFantasia { get; private set; }    // Nome de exibição
    public string Cnpj { get; private set; }            // 14 dígitos, único
    public string Email { get; private set; }           // Login da empresa
    public string Senha { get; private set; }           // Hash BCrypt
    public string? Telefone { get; private set; }
    public string? LogoUrl { get; private set; }        // Branding próprio
    public string? Descricao { get; private set; }
    public string? Site { get; private set; }
    public PlanoEmpresa Plano { get; private set; } = PlanoEmpresa.Gratuito;
    public bool Ativo { get; private set; } = true;
    public DateTime DataCriacao { get; private set; } = DateTime.Now;

    // Factory method
    public static Empresa Criar(string nome, string nomeFantasia, string cnpj,
                                 string email, string senha, string? telefone) { ... }
}
```

### 5.2 [`Evento`](../Domain/Entities/Evento.cs) — ATUALIZADO

```csharp
public class Evento
{
    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }         // ← Quem é dono do evento
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }       // ← NOVO
    public string? Local { get; private set; }            // ← NOVO
    public string? ImagemUrl { get; private set; }        // ← NOVO
    public TipoEvento Tipo { get; private set; }          // ← NOVO: Teatro ou Palestra
    public int CapacidadeTotal { get; private set; }
    public DateTime DataEvento { get; private set; }
    public decimal PrecoPadrao { get; private set; }     // 0 = gratuito
    public bool Gratuito => PrecoPadrao == 0;             // ← Propriedade calculada
    public DateTime DataCriacao { get; private set; }

    public List<Ingresso> Ingressos { get; private set; } = new();
}
```

### 5.3 [`TipoEvento`](../Domain/Entities/TipoEvento.cs) — NOVO ENUM

```csharp
public enum TipoEvento
{
    Teatro = 0,     // Assentos numerados com filas e setores
    Palestra = 1,   // Lotação geral (sem assento fixo)
}
```

### 5.4 [`Usuario`](../Domain/Entities/Usuario.cs) — MANTIDO + Ativo

```csharp
public class Usuario
{
    public string Cpf { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string Senha { get; private set; }           // Com hash (BCrypt)
    public Guid PerfilId { get; private set; }           // Admin ou Comprador apenas
    public bool Ativo { get; private set; } = true;      // ← NOVO
}
```

### 5.5 [`Ingresso`](../Domain/Entities/Ingresso.cs) — ATUALIZADO

```csharp
public class Ingresso
{
    public Guid Id { get; private set; }
    public Guid EventoId { get; private set; }
    public decimal Preco { get; private set; }
    public string? Posicao { get; private set; }         // ← nullable (Palestra não tem)
    public string? Setor { get; private set; }           // ← nullable (Palestra não tem)
    public int Status { get; private set; }              // 0=Livre, 1=Reservado, 2=Vendido
    public DateTime? DataBloqueio { get; private set; }
}
```

### 5.6 [`Reserva`](../Domain/Entities/Reserva.cs) — ATUALIZADO

```csharp
public class Reserva
{
    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }          // ← NOVO: dona do evento
    public string UsuarioCpf { get; private set; }
    public Guid EventoId { get; private set; }
    public Guid? IngressoId { get; private set; }        // ← nullable (Palestra)
    public int Quantidade { get; private set; } = 1;     // ← NOVO (Palestra)
    public string? CupomUtilizado { get; private set; }
    public decimal ValorFinalPago { get; private set; }
}
```

### 5.7 [`Cupom`](../Domain/Entities/Cupom.cs) — ATUALIZADO

```csharp
public class Cupom
{
    public string Codigo { get; private set; }
    public Guid EmpresaId { get; private set; }          // ← NOVO: dono do cupom
    public int PorcentagemDesconto { get; private set; }
    public decimal ValorMinimo { get; private set; }
    public DateTime? DataExpiracao { get; private set; }
    public bool Ativo { get; private set; }
}
```

### 5.8 [`PlanoEmpresa`](../Domain/Entities/PlanoEmpresa.cs) — NOVO ENUM

```csharp
public enum PlanoEmpresa
{
    Gratuito = 0,     // Limitado: até 3 eventos, sem cupons
    Basico = 1,       // Até 10 eventos/mês, cupons ilimitados
    Profissional = 2, // Eventos ilimitados, relatórios, branding próprio
}
```

---

## 6. Tipos de Evento

### 6.1 Teatro — Assentos Numerados

**Comportamento (mantido do sistema atual):**
- Geração automática de `N` ingressos no momento da criação do evento
- 10% VIP (preço × 1.5), 90% Geral (preço padrão)
- 20 assentos por fila
- Posição: `"Fila X | Assento Y"`
- Comprador seleciona assento específico no mapa visual
- 1 reserva = 1 ingresso (`IngressoId` preenchido)

### 6.2 Palestra — Lotação Geral

**Comportamento novo:**
- **Não gera ingressos individuais** — apenas um contador de vagas
- Comprador seleciona **quantidade de vagas** (1 a N)
- `IngressoId` na reserva fica `null`
- `Quantidade` na reserva = número de vagas
- Controle: `VagasDisponiveis = CapacidadeTotal - SUM(Quantidade reservada)`

### 6.3 Comparativo

| Característica | Teatro | Palestra |
|---|---|---|
| Assento numerado | ✅ Sim | ❌ Não |
| Mapa visual de assentos | ✅ Sim | ❌ Não |
| Seleção de quantidade | ❌ 1 assento por vez | ✅ Múltiplas vagas |
| Geração de ingressos | ✅ Automática (N ingressos) | ❌ Apenas controle |
| IngressoId na reserva | ✅ Preenchido | ❌ Null |
| Preço por unidade | ✅ Sim | ✅ Sim |
| Gratuito | ✅ Sim | ✅ Sim |

---

## 7. Modelos de Precificação

### 7.1 Evento Pago (`PrecoPadrao > 0`)

- Fluxo normal: seleção → checkout → pagamento → confirmação
- Ingresso marcado como **Vendido** (Status=2) após confirmação
- Background Worker libera assentos não pagos após 15 minutos

### 7.2 Evento Gratuito (`PrecoPadrao = 0`)

- Fluxo simplificado: seleção → confirmação direta
- **Pula a tela de pagamento**
- Ingresso vai direto para **Vendido** (Status=2)
- `ValorFinalPago = 0`
- **Cupons não se aplicam** (desconto sobre zero = zero)

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
POST /api/usuario/cadastrar → conta criada
    ↓
[Login.razor] Email + Senha
    ↓
POST /api/usuario/login → JWT { role: Comprador, cpf, email }
    ↓
Redireciona para Home
```

### 8.2 Fluxo de Cadastro e Login — EMPRESA

```
[Página Pública] Home.razor (botão "Sou Empresa")
    ↓
[EmpresaCadastro.razor] Preenche CNPJ, Nome, Nome Fantasia, Email, Senha, Telefone
    ↓
POST /api/empresa/cadastrar → empresa criada (Plano Gratuito)
    ↓
[EmpresaLogin.razor] Email + Senha
    ↓
POST /api/empresa/login → JWT { role: Empresa, empresaId, cnpj }
    ↓
Redireciona para Painel da Empresa
```

### 8.3 Fluxo de Compra — TEATRO

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

### 8.4 Fluxo de Compra — PALESTRA

```
Home → Clica no evento
    ↓
Tela do evento (detalhes, sem mapa, mostra vagas disponíveis)
    ↓ Seleciona quantidade de vagas (1 a N)
Checkout (aplica cupom opcional)
    ↓
É gratuito? ── Sim ──→ Reserva confirmada
    ↓ Não
Pagamento → Confirmar → Reserva criada
```

### 8.5 Fluxo do ADMIN (Plataforma)

```
Login: POST /api/usuario/login (cpf=000..., admin)
    ↓
Dashboard Admin:
├── Empresas (listar, ativar/desativar, alterar plano)
├── Usuários Compradores (listar)
├── Eventos (visão geral de todos)
└── Suporte
```

---

## 9. Requisitos Funcionais

### 9.1 Empresa

| ID | Requisito | Prioridade |
|---|---|---|
| EMP-01 | Empresa se cadastra com CNPJ, Nome, Nome Fantasia, Email, Senha, Telefone | Alta |
| EMP-02 | Sistema valida CNPJ (formato + dígitos verificadores + unicidade) | Alta |
| EMP-03 | Sistema valida email (formato + unicidade entre empresas) | Alta |
| EMP-04 | Empresa faz login com email + senha → JWT com role=Empresa | Alta |
| EMP-05 | Empresa pode editar seus dados (nome, logo, descrição, site) | Média |
| EMP-06 | Admin ativa/desativa empresa | Alta |
| EMP-07 | Admin altera plano da empresa | Média |
| EMP-08 | Empresa inativa não pode fazer login nem criar/editar eventos | Alta |

### 9.2 Evento

| ID | Requisito | Prioridade |
|---|---|---|
| EVT-01 | Empresa cria evento vinculado a ela (EmpresaId) | Alta |
| EVT-02 | Ao criar, escolhe tipo: Teatro (0) ou Palestra (1) | Alta |
| EVT-03 | Teatro: sistema gera N ingressos (10% VIP, 90% Geral) | Alta |
| EVT-04 | Palestra: não gera ingressos, usa controle de vagas | Alta |
| EVT-05 | Evento pode ser gratuito (PrecoPadrao = 0) | Alta |
| EVT-06 | Empresa adiciona descrição, local, imagem | Média |
| EVT-07 | Empresa edita apenas seus próprios eventos | Alta |
| EVT-08 | Admin pode ver/excluir qualquer evento | Média |
| EVT-09 | Eventos de empresas inativas não aparecem na Home | Alta |

### 9.3 Reserva / Compra

| ID | Requisito | Prioridade |
|---|---|---|
| RES-01 | Teatro: comprador seleciona 1 assento específico | Alta |
| RES-02 | Palestra: comprador seleciona quantidade (1 a N) | Alta |
| RES-03 | Sistema valida vagas disponíveis antes de confirmar | Alta |
| RES-04 | Palestra: impede ultrapassar CapacidadeTotal | Alta |
| RES-05 | Gratuito: pula pagamento, confirma imediatamente | Média |
| RES-06 | Cupom não pode ser aplicado em evento gratuito | Média |
| RES-07 | Reserva vinculada à empresa do evento | Alta |
| RES-08 | Background Worker libera assentos expirados por empresa | Alta |

### 9.4 Cupom

| ID | Requisito | Prioridade |
|---|---|---|
| CUP-01 | Cupom pertence a uma empresa (EmpresaId) | Alta |
| CUP-02 | Empresa gerencia seus próprios cupons (CRUD) | Alta |
| CUP-03 | Cupom válido apenas para eventos da mesma empresa | Alta |
| CUP-04 | Cupom não se aplica a eventos gratuitos | Média |

### 9.5 Comprador

| ID | Requisito | Prioridade |
|---|---|---|
| COM-01 | Comprador se cadastra com CPF, Nome, Email, Senha | Alta |
| COM-02 | Comprador faz login → JWT com role=Comprador | Alta |
| COM-03 | Comprador altera nome, email, senha | Média |
| COM-04 | Comprador vê histórico de reservas | Alta |
| COM-05 | Comprador remove própria conta | Média |

### 9.6 Admin

| ID | Requisito | Prioridade |
|---|---|---|
| ADM-01 | Admin cadastrado manualmente no banco (seed) | Alta |
| ADM-02 | Admin faz login pelo mesmo endpoint de usuário | Alta |
| ADM-03 | Admin lista todas empresas | Alta |
| ADM-04 | Admin ativa/desativa empresa | Alta |
| ADM-05 | Admin altera plano da empresa | Média |
| ADM-06 | Admin lista todos compradores | Média |
| ADM-07 | Admin visualiza eventos de qualquer empresa | Média |

---

## 10. Requisitos Não Funcionais

| ID | Requisito |
|---|---|
| RNF-01 | Senhas armazenadas com hash (BCrypt) — Admin, Comprador e Empresa |
| RNF-02 | Autenticação via JWT com roles: Admin, Comprador, Empresa |
| RNF-03 | Isolamento de dados: toda query filtra por EmpresaId |
| RNF-04 | Proteção contra SQL Injection (Dapper parametrizado) — mantido |
| RNF-05 | Migrations versionadas (DbUp) — mantido |
| RNF-06 | Operações críticas em transações — mantido |
| RNF-07 | Background Worker filtra por empresa (libera apenas assentos da empresa correta) |
| RNF-08 | CNPJ validado com dígitos verificadores (regra de negócio no Domain) |

---

## 11. Banco de Dados

### 11.1 Nova Tabela: [`Empresas`](../Infraestructure/DataBase/Scripts/Script0009_CriarEmpresas.sql)

```sql
CREATE TABLE Empresas (
    Id              UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Nome            VARCHAR(200)        NOT NULL,
    NomeFantasia    VARCHAR(200)        NOT NULL,
    Cnpj            VARCHAR(14)         NOT NULL UNIQUE,
    Email           VARCHAR(200)        NOT NULL,
    Senha           VARCHAR(200)        NOT NULL,
    Telefone        VARCHAR(20)         NULL,
    LogoUrl         VARCHAR(500)        NULL,
    Descricao       VARCHAR(1000)       NULL,
    Site            VARCHAR(500)        NULL,
    Plano           INT                 NOT NULL DEFAULT 0,
    Ativo           BIT                 NOT NULL DEFAULT 1,
    DataCriacao     DATETIME            NOT NULL DEFAULT GETDATE()
);
```

### 11.2 Alterações em Tabelas Existentes

```sql
-- ============================================
-- Eventos: adicionar colunas
-- ============================================
ALTER TABLE Eventos ADD
    EmpresaId       UNIQUEIDENTIFIER    NOT NULL,
    Descricao       VARCHAR(2000)       NULL,
    Local           VARCHAR(500)        NULL,
    ImagemUrl       VARCHAR(500)        NULL,
    Tipo            INT                 NOT NULL DEFAULT 0,
    DataCriacao     DATETIME            NOT NULL DEFAULT GETDATE();

ALTER TABLE Eventos ADD CONSTRAINT FK_Eventos_Empresas
    FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id);
CREATE INDEX IX_Eventos_EmpresaId ON Eventos(EmpresaId);

-- ============================================
-- Cupons: adicionar EmpresaId
-- ============================================
ALTER TABLE Cupons ADD
    EmpresaId       UNIQUEIDENTIFIER    NOT NULL;
ALTER TABLE Cupons ADD CONSTRAINT FK_Cupons_Empresas
    FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id);
CREATE INDEX IX_Cupons_EmpresaId ON Cupons(EmpresaId);

-- ============================================
-- Reservas: adicionar colunas + alterar IngressoId
-- ============================================
ALTER TABLE Reservas ADD
    EmpresaId       UNIQUEIDENTIFIER    NOT NULL,
    Quantidade      INT                 NOT NULL DEFAULT 1;
ALTER TABLE Reservas ALTER COLUMN IngressoId UNIQUEIDENTIFIER NULL;
ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Empresas
    FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id);
CREATE INDEX IX_Reservas_EmpresaId ON Reservas(EmpresaId);

-- ============================================
-- Usuarios: adicionar Ativo (já existe?)
-- ============================================
ALTER TABLE Usuarios ADD
    Ativo           BIT                 NOT NULL DEFAULT 1;

-- ============================================
-- Perfis: remover Vendedor
-- ============================================
DELETE FROM Perfis WHERE Nome = 'Vendedor';
-- Perfis restantes: Admin (A1A1...), Comprador (C3C3...)
```

### 11.3 Relacionamentos

```
Empresas (1) ──── (N) Eventos
Empresas (1) ──── (N) Cupons
Empresas (1) ──── (N) Reservas (via evento)

Eventos (1) ──── (N) Ingressos (apenas Teatro)
Eventos (1) ──── (N) Reservas

Usuarios (1) ──── (N) Reservas (como comprador)
Ingressos (1) ──── (1) Reservas (apenas Teatro)

Reservas (1) ──── (0..1) Pagamentos

Perfis (1) ──── (N) Usuarios
```

---

## 12. APIs e Endpoints

### 12.1 Usuário (Admin + Comprador)

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/usuario/cadastrar` | Cadastro de comprador | ❌ Público |
| `POST` | `/api/usuario/login` | Login (admin ou comprador) | ❌ Público |
| `GET`  | `/api/usuario/{cpf}` | Dados do usuário | ✅ JWT |
| `PUT`  | `/api/usuario/alterar-nome` | Alterar nome | ✅ JWT |
| `PUT`  | `/api/usuario/alterar-email` | Alterar email | ✅ JWT |
| `PUT`  | `/api/usuario/alterar-senha` | Alterar senha | ✅ JWT |
| `DELETE` | `/api/usuario/{cpf}` | Remover conta | ✅ JWT |

### 12.2 Empresa

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/empresa/cadastrar` | Cadastro de empresa | ❌ Público |
| `POST` | `/api/empresa/login` | Login da empresa | ❌ Público |
| `GET`  | `/api/empresa/dados` | Dados da empresa logada | ✅ JWT Empresa |
| `PUT`  | `/api/empresa/atualizar` | Atualizar dados | ✅ JWT Empresa |
| `PUT`  | `/api/empresa/logo` | Atualizar logo | ✅ JWT Empresa |

### 12.3 Evento

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/evento/criar` | Criar evento (com Tipo) | ✅ JWT Empresa |
| `GET`  | `/api/evento/listar` | Lista pública de eventos | ❌ Público |
| `GET`  | `/api/evento/empresa` | Eventos da empresa logada | ✅ JWT Empresa |
| `GET`  | `/api/evento/{id}` | Detalhes do evento | ❌ Público |
| `PUT`  | `/api/evento/{id}` | Editar evento | ✅ JWT Empresa |
| `DELETE` | `/api/evento/{id}` | Excluir evento | ✅ JWT Empresa/Admin |

### 12.4 Reserva

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/reserva/criar` | Criar reserva (com Quantidade opcional) | ✅ JWT Comprador |
| `GET`  | `/api/reserva/minhas` | Reservas do comprador | ✅ JWT Comprador |
| `GET`  | `/api/reserva/evento/{eventoId}` | Reservas de um evento | ✅ JWT Empresa/Admin |

### 12.5 Cupom

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `POST` | `/api/cupom/cadastrar` | Cadastrar cupom | ✅ JWT Empresa |
| `GET`  | `/api/cupom/empresa` | Cupons da empresa | ✅ JWT Empresa |
| `DELETE` | `/api/cupom/{codigo}` | Remover cupom | ✅ JWT Empresa |

### 12.6 Admin

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| `GET`  | `/api/admin/empresas` | Listar empresas | ✅ JWT Admin |
| `GET`  | `/api/admin/empresa/{id}` | Dados de empresa | ✅ JWT Admin |
| `PUT`  | `/api/admin/empresa/{id}/plano` | Alterar plano | ✅ JWT Admin |
| `PUT`  | `/api/admin/empresa/{id}/ativar` | Ativar/desativar | ✅ JWT Admin |
| `GET`  | `/api/admin/usuarios` | Listar compradores | ✅ JWT Admin |

---

## 13. Plano de Migração

### Fase 1 — Fundação (Domain + Infra)

| # | Tarefa | Arquivos |
|---|--------|----------|
| 1 | Criar entidade [`Empresa`](../Domain/Entities/Empresa.cs) | `Domain/Entities/Empresa.cs` |
| 2 | Criar enum [`TipoEvento`](../Domain/Entities/TipoEvento.cs) | `Domain/Entities/TipoEvento.cs` |
| 3 | Criar enum [`PlanoEmpresa`](../Domain/Entities/PlanoEmpresa.cs) | `Domain/Entities/PlanoEmpresa.cs` |
| 4 | Criar interface [`IEmpresaRepository`](../Domain/Interface/IEmpresaRepository.cs) | `Domain/Interface/IEmpresaRepository.cs` |
| 5 | Atualizar [`Evento`](../Domain/Entities/Evento.cs): adicionar `EmpresaId`, `Tipo`, `Descricao`, `Local`, `ImagemUrl` | `Domain/Entities/Evento.cs` |
| 6 | Atualizar [`Usuario`](../Domain/Entities/Usuario.cs): adicionar `Ativo` | `Domain/Entities/Usuario.cs` |
| 7 | Atualizar [`Reserva`](../Domain/Entities/Reserva.cs): adicionar `EmpresaId`, `Quantidade`, `IngressoId` nullable | `Domain/Entities/Reserva.cs` |
| 8 | Atualizar [`Cupom`](../Domain/Entities/Cupom.cs): adicionar `EmpresaId` | `Domain/Entities/Cupom.cs` |
| 9 | Atualizar [`Ingresso`](../Domain/Entities/Ingresso.cs): `Posicao`/`Setor` nullable | `Domain/Entities/Ingresso.cs` |
| 10 | Simplificar [`Perfil`](../Domain/Entities/Perfil.cs): remover Vendedor | `Domain/Entities/Perfil.cs` |
| 11 | Criar script SQL `Script0009_CriarEmpresas.sql` | `Infraestructure/DataBase/Scripts/` |
| 12 | Criar `EmpresaRepository` | `Infraestructure/Repository/EmpresaRepository.cs` |

### Fase 2 — Application

| # | Tarefa | Arquivos |
|---|--------|----------|
| 1 | Criar `EmpresaService` (cadastro, login, CRUD) | `Application/Service/EmpresaService.cs` |
| 2 | Criar `IEmpresaService` | `Application/Interfaces/IEmpresaService.cs` |
| 3 | Atualizar `TokenService` para gerar JWT com role=Empresa | `Application/Service/TokenService.cs` |
| 4 | Atualizar `EventoService`: filtrar por EmpresaId, suportar Tipo | `Application/Service/EventoService.cs` |
| 5 | Atualizar `ReservaService`: suportar Palestra (Quantidade), validar vagas | `Application/Service/ReservaService.cs` |
| 6 | Atualizar `CupomService`: filtrar por EmpresaId | `Application/Service/CupomService.cs` |
| 7 | Criar DTOs de Empresa | `Application/DTOs/Empresa*.cs` |
| 8 | Atualizar `EventoRequestDTO`: adicionar Tipo, Descricao, Local | `Application/DTOs/EventoRequestDTO.cs` |

### Fase 3 — API

| # | Tarefa | Arquivos |
|---|--------|----------|
| 1 | Criar `EmpresaController` (cadastro, login, dados) | `Api/Controllers/EmpresaController.cs` |
| 2 | Atualizar `EventoController`: filtrar por empresa logada | `Api/Controllers/EventoController.cs` |
| 3 | Atualizar `ReservaController`: Palestra e Quantidade | `Api/Controllers/ReservaController.cs` |
| 4 | Atualizar `Program.cs`: registrar novos serviços | `Api/Program.cs` |

### Fase 4 — Frontend

| # | Tarefa | Arquivos |
|---|--------|----------|
| 1 | Criar página de cadastro de empresa | `Web/Components/Pages/EmpresaCadastro.razor` |
| 2 | Criar página de login de empresa | `Web/Components/Pages/EmpresaLogin.razor` |
| 3 | Criar painel da empresa (dashboard) | `Web/Components/Pages/Empresa/` |
| 4 | Atualizar [`CriarEvento.razor`](../Web/Components/Pages/Vendedor/CriarEvento.razor): Tipo, nova logo | Web |
| 5 | Criar tela de detalhes para Palestra (quantidade, sem mapa) | Web |
| 6 | Atualizar [`ComprarIngressos.razor`](../Web/Components/Pages/ComprarIngressos.razor) sem mapa para Palestra | Web |
| 7 | Criar dashboard Admin com gestão de empresas | `Web/Components/Pages/Admin/` |
| 8 | Atualizar [`Home.razor`](../Web/Components/Pages/Home.razor): mostrar empresa do evento | Web |
| 9 | Adicionar navegação para login/cadastro de empresa | Web |
| 10 | Ajustar `CustomAuthStateProvider` para suportar role=Empresa | `Web/Auth/CustomAuthStateProvider.cs` |

---

## 14. Glossário

| Termo | Definição |
|-------|-----------|
| **Admin** | Administrador da plataforma. Cadastrado manualmente no banco (seed). Gerencia empresas. |
| **Comprador** | Pessoa física que se cadastra pelo site para comprar ingressos. |
| **Empresa** | Pessoa jurídica que se cadastra pelo site para criar e vender eventos. Possui login próprio. |
| **Evento** | Atividade (teatro, palestra, show) criada por uma Empresa. |
| **Teatro** | Tipo de evento com assentos numerados, filas, setores (VIP/Geral) e mapa visual. |
| **Palestra** | Tipo de evento com lotação geral, sem assentos fixos, apenas controle de vagas. |
| **Evento Gratuito** | Evento com PrecoPadrao = 0. Não requer pagamento, confirma imediatamente. |
| **Ingresso** | Representação individual de um assento (apenas para Teatro). |
| **Reserva** | Vínculo entre Comprador e um evento/ingresso, com ou sem cupom. |
| **Cupom** | Desconto percentual vinculado a uma Empresa, aplicável em seus eventos pagos. |
| **Plano** | Nível de assinatura da Empresa: Gratuito, Básico ou Profissional. |

---

> **Documento v1.0** — Especificações para evolução do SoldOut Tickets para SaaS.
>
> **Três entidades:**
> - **Admin** → cadastro manual no banco (seed)
> - **Comprador** → cadastro pelo site (CPF)
> - **Empresa** → cadastro pelo site (CNPJ) — NOVO
>
> **Dois perfis de usuário:**
> - Admin (A1A1...)
> - Comprador (C3C3...)
>
> **Empresa é entidade própria** → tabela `Empresas`, login próprio, JWT próprio, dona dos eventos e cupons.
