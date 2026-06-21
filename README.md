# ARTHUR REZENDE DE OLIVEIRA - 06010228
# IAN CARLOS DE OLIVEIRA LEITE -06012992
# EDUARDO LEAL - 06013706
# ERICK LOPES DOS SANTOS CARVALHO - 06010632

# SoldOut Tickets

Sistema de venda de ingressos para eventos, desenvolvido com ASP.NET Core, Blazor Server e SQL Server.

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server ou SQL Server Express (para desenvolvimento local)
- [Docker](https://www.docker.com/) (opcional, para deploy containerizado)

---

## Rodando com Docker (recomendado)

```bash
# Subir ambiente completo (API + Web + SQL Server)
docker-compose up -d

# Ver logs da API
docker-compose logs -f api

# Ver logs do frontend
docker-compose logs -f web

# Parar
docker-compose down

# Recriar do zero (limpa banco)
docker-compose down -v && docker-compose up -d
```

Após subir:
- **API + Swagger:** http://localhost:5007/swagger
- **Dashboard Hangfire (Admin):** http://localhost:5007/hangfire
- **Frontend:** http://localhost:5057

---

## Rodando localmente (sem Docker)

### Configuração do Banco de Dados

Abra o arquivo `Api/appsettings.json` e configure a connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ProjetoTicketDB;Trusted_Connection=True;TrustServerCertificate=True;Max Pool Size=100;"
}
```

> O banco e todas as tabelas são criados automaticamente ao rodar a API pela primeira vez.

### Configuração do JWT

Em desenvolvimento, configure a chave JWT via user-secrets:

```bash
cd Api
dotnet user-secrets set "Jwt:Key" "sua-chave-secreta-de-32-caracteres"
dotnet user-secrets set "Jwt:Issuer" "ProjetoTicket"
dotnet user-secrets set "Jwt:Audience" "ProjetoTicket"
```

### Rodando a API

```bash
cd Api
dotnet run
```

A API estará disponível em: `http://localhost:5007`  
Swagger disponível em: `http://localhost:5007/swagger`  
Dashboard Hangfire (Admin): `http://localhost:5007/hangfire`

### Rodando o Frontend

```bash
cd Web
dotnet run
```

O frontend estará disponível em: `http://localhost:5057`

### Rodando os Testes

```bash
cd tests
dotnet test
```

### Restaurando dependências

```bash
dotnet restore
```

### Compilando o projeto

```bash
dotnet build
```

---

## Estrutura do Projeto

```
ProjetoTicket/
├── Api/               → Controllers, Middlewares, BackgroundTasks (Hangfire), Program.cs
├── Application/       → Services, DTOs, Interfaces, Mappings
├── Domain/            → Entities, Exceptions, Interfaces
├── Infraestructure/   → Repositories, Migrations, Database
├── Web/               → Frontend Blazor Server
├── docs/              → Documentação (requisitos, arquitetura, sprints)
├── db/                → Scripts SQL
├── tests/             → Testes automatizados xUnit
├── Dockerfile         → Dockerfile da API (multi-stage build)
├── Web.Dockerfile     → Dockerfile do Frontend Blazor
└── docker-compose.yml → Orquestração (API + Web + SQL Server)
```

## Perfis de Usuário

| Perfil | Permissões |
|---|---|
| Admin | Gerenciar usuários, eventos, cupons e acessar dashboard Hangfire |
| Vendedor | Criar e gerenciar seus próprios eventos |
| Comprador | Visualizar eventos e realizar reservas |

## Stack Tecnológica

| Tecnologia | Uso |
|---|---|
| .NET 9 | Runtime principal |
| Dapper | ORM / acesso a dados |
| SQL Server | Banco de dados |
| DbUp | Migrations versionadas |
| JWT (Bearer) | Autenticação |
| BCrypt | Hash de senhas |
| MudBlazor | Componentes UI |
| AutoMapper | Mapeamento de DTOs |
| Hangfire | Jobs em background (liberação de assentos, dashboard de monitoramento) |
| Docker | Containerização |