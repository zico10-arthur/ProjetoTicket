<div align="center">

# ProjetoTicket — SoldOut Tickets

**E-commerce de ingressos para eventos: cadastro de eventos, perfis de acesso e compra com controle de disponibilidade.**

![.NET](https://img.shields.io/badge/.NET%209-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-512BD4?style=flat-square&logo=csharp&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor%20Server-512BD4?style=flat-square&logo=blazor&logoColor=white)
![MudBlazor](https://img.shields.io/badge/MudBlazor-594AE2?style=flat-square&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)

</div>

---

## 📌 Sobre o projeto

Sistema de venda de ingressos para eventos, construído para **modernizar uma plataforma legada**. O projeto cobre o fluxo completo: o vendedor cadastra o evento, o comprador reserva o ingresso e o sistema controla a disponibilidade em tempo real, com autenticação e autorização por perfil.

### Funcionalidades

- Autenticação e autorização por perfil (Admin, Vendedor, Comprador)
- Cadastro e gestão de eventos
- Reserva/compra de ingressos com controle de disponibilidade
- Cupons de desconto
- API REST documentada via Swagger
- Dashboard administrativo com Hangfire
- Testes automatizados com xUnit

---

## 🏗️ Arquitetura

Organização em camadas, com separação de responsabilidades:

| Camada | Responsabilidade |
|---|---|
| `Api` | Controllers, middlewares e configuração da aplicação |
| `Application` | Services, DTOs, interfaces e mappings |
| `Domain` | Entidades, exceções e contratos de domínio |
| `Infraestructure` | Repositórios, migrations e acesso ao banco |
| `Web` | Frontend Blazor Server com componentes MudBlazor |
| `tests` | Testes automatizados (xUnit) |
| `db` | Scripts SQL |
| `docs` | Requisitos, arquitetura e operação |

---

## 🧰 Stack

| Área | Tecnologia |
|---|---|
| Plataforma | .NET 9 · ASP.NET Core |
| Frontend | Blazor Server · MudBlazor |
| Banco de dados | SQL Server (criação automática do schema na primeira execução) |
| Containerização | Docker · Docker Compose |
| Tarefas em background | Hangfire |
| Testes | xUnit |
| Documentação de API | Swagger |

---

## 🚀 Rodando com Docker (recomendado)

```bash
# Subir ambiente completo (API + Web + SQL Server)
docker-compose up -d

# Ver logs da API
docker-compose logs -f api

# Parar
docker-compose down

# Recriar do zero (limpa banco)
docker-compose down -v && docker-compose up -d
```

- **API:** `http://localhost:5007`
- **Swagger:** `http://localhost:5007/swagger`
- **Frontend:** `http://localhost:5057`
- **Dashboard Hangfire (Admin):** `http://localhost:5007/hangfire`

---

## ⚙️ Rodando localmente (sem Docker)

**Pré-requisitos**

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server ou SQL Server Express

**Banco de dados** — configure a connection string em `Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ProjetoTicketDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> O banco e as tabelas são criados automaticamente na primeira execução da API.

**API**

```bash
cd Api
dotnet run
```

Disponível em `http://localhost:5007` — Swagger em `http://localhost:5007/swagger`.

**Frontend**

```bash
cd Web
dotnet run
```

Disponível em `http://localhost:5057`.

**Testes**

```bash
cd tests
dotnet test
```

**Build**

```bash
dotnet restore
dotnet build
```

---

## 🐳 Deploy com Docker

O projeto inclui configuração completa para containerização:

| Arquivo | Função |
|---------|--------|
| `Api/Dockerfile` | Imagem da API (.NET 9, multi-stage build) |
| `Web/Dockerfile` | Imagem do Frontend Blazor Server (.NET 9, multi-stage build) |
| `docker-compose.yml` | Orquestração: SQL Server + API + Web |

**Pré-requisitos:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/Mac) ou Docker Engine (Linux).

```bash
# Subir tudo
docker-compose up -d

# Subir reconstruindo imagens (após mudanças no código)
docker-compose up -d --build

# Ver status
docker-compose ps

# Logs de um serviço específico
docker-compose logs -f api

# Parar tudo
docker-compose down

# Parar e limpar volumes (banco zerado)
docker-compose down -v
```

---

## 👥 Perfis de usuário

| Perfil | Permissões |
|---|---|
| Admin | Gerenciar usuários, eventos e cupons |
| Vendedor | Criar e gerenciar seus próprios eventos |
| Comprador | Visualizar eventos e realizar reservas |

---

## 📁 Estrutura do repositório

```text
ProjetoTicket/
├── Api/              → Controllers, Middlewares, Program.cs
├── Application/      → Services, DTOs, Interfaces, Mappings
├── Domain/           → Entities, Exceptions, Interfaces
├── Infraestructure/  → Repositories, Migrations, Database
├── Web/              → Frontend Blazor Server
├── docs/             → Documentação (requisitos, arquitetura, operação)
├── db/               → Scripts SQL
└── tests/            → Testes automatizados xUnit
```

---

## 👤 Autores

- **Arthur Rezende de Oliveira** — [@zico10-arthur](https://github.com/zico10-arthur)
- **Eduardo Leal** — [@dudu4leal](https://github.com/dudu4leal)
- **Erick Lopes dos Santos Carvalho** — [@ErickLopesSC](https://github.com/ErickLopesSC)
- **Ian Carlos de Oliveira Leite** — [@Ianclrs](https://github.com/Ianclrs)
- **Yuri Domingues** — [@yuridomingues](https://github.com/yuridomingues)
