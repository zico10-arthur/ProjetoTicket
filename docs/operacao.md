# Como Rodar o Projeto — SoldOut Tickets

## Pré-requisitos

- .NET 9 SDK
- SQL Server (ou SQL Server Express)
- Visual Studio 2022 ou VS Code

## Configuração do Banco de Dados

1. Abra o arquivo `Api/appsettings.json`
2. Atualize a connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ProjetoTicketDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> O banco e as tabelas são criados automaticamente pelo DbUp ao rodar a API.

## Rodando a API

```bash
cd Api
dotnet run
```

A API estará disponível em `http://localhost:5007`  
Swagger disponível em `http://localhost:5007/swagger`

## Rodando o Frontend

```bash
cd Web
dotnet run
```

O frontend estará disponível em `http://localhost:5057`

## Usuários Padrão

Após rodar a API, cadastre um usuário Admin diretamente no banco:

```sql
INSERT INTO Usuarios (Cpf, Nome, Email, PerfilId, Senha)
VALUES ('00000000000', 'Admin', 'admin@admin.com', 
        'A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1', 'Senha@123')
```

## Perfis disponíveis

| PerfilId | Role |
|---|---|
| A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1 | Admin |
| B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2 | Vendedor |
| C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3 | Comprador |

## Rodando os Testes

```bash
cd tests
dotnet test
```
