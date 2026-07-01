# ARTHUR REZENDE DE OLIVEIRA - 06010228
# EDUARDO LEAL - 06013706
# ERICK LOPES DOS SANTOS CARVALHO - 06010632
# IAN CARLOS DE OLIVEIRA LEITE - 06012992
# YURI DOMINGUES - 06010142

# SoldOut Tickets

Sistema de venda de ingressos para eventos, desenvolvido com ASP.NET Core, Blazor Server e SQL Server.

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server ou SQL Server Express

## Configuração do Banco de Dados

Abra o arquivo `Api/appsettings.json` e configure a connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ProjetoTicketDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> O banco e todas as tabelas são criados automaticamente ao rodar a API pela primeira vez.

## Rodando com Docker (recomendado)

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

## Rodando localmente (sem Docker)

### Rodando a API

```bash
cd Api
dotnet run
```

A API estará disponível em: `http://localhost:5007`  
Swagger disponível em: `http://localhost:5007/swagger`

### Rodando o Frontend

```bash
cd Web
dotnet run
```

O frontend estará disponível em: `http://localhost:5057`

## Rodando os Testes

```bash
cd tests
dotnet test
```

## Restaurando dependências

```bash
dotnet restore
```

## Compilando o projeto

```bash
dotnet build
```

## Deploy com Docker

O projeto inclui configuração completa para containerização:

| Arquivo | Função |
|---------|--------|
| `Api/Dockerfile` | Imagem da API (.NET 9, multi-stage build) |
| `Web/Dockerfile` | Imagem do Frontend Blazor Server (.NET 9, multi-stage build) |
| `docker-compose.yml` | Orquestração: SQL Server + API + Web |

### Pré-requisitos Docker

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/Mac) ou Docker Engine (Linux)

### Comandos úteis

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

## Estrutura do Projeto

```
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

## Perfis de Usuário

| Perfil | Permissões |
|---|---|
| Admin | Gerenciar usuários, eventos e cupons |
| Vendedor | Criar e gerenciar seus próprios eventos |
| Comprador | Visualizar eventos e realizar reservas |
