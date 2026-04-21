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

## Rodando a API

```bash
cd Api
dotnet run
```

A API estará disponível em: `http://localhost:5007`  
Swagger disponível em: `http://localhost:5007/swagger`

## Rodando o Frontend

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
