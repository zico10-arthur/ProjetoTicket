# Arquitetura do Sistema — SoldOut Tickets

## Visão Geral

O sistema segue o padrão **Clean Architecture**, dividido em 4 camadas:

```
ProjetoTicket/
├── Api/              → Controllers, Middlewares, Program.cs
├── Application/      → Services, DTOs, Interfaces, Mappings
├── Domain/           → Entities, Exceptions, Interfaces de Repositório
├── Infraestructure/  → Repositories, ConnectionFactory, Migrations
└── Web/              → Frontend Blazor Server (MudBlazor)
```

## Camadas

### Domain
- Entidades com regras de negócio encapsuladas (Usuario, Evento, Ingresso, Reserva, Cupom)
- Exceções tipadas de domínio
- Interfaces de repositório (contratos)

### Application
- Serviços de aplicação (casos de uso)
- DTOs de entrada e saída
- Mapeamentos AutoMapper
- Interfaces de serviço

### Infrastructure
- Implementação dos repositórios com Dapper
- ConnectionFactory para SQL Server
- Migrations com DbUp
- Background Worker para liberar assentos expirados

### Api
- Controllers REST
- Middleware global de exceções
- Configuração JWT
- Injeção de dependências (Program.cs)

## Tecnologias

| Tecnologia | Uso |
|---|---|
| .NET 9 | Plataforma |
| ASP.NET Core | API REST |
| Blazor Server | Frontend |
| Dapper | ORM leve |
| SQL Server | Banco de dados |
| JWT Bearer | Autenticação |
| AutoMapper | Mapeamento de objetos |
| DbUp | Migrations |
| MudBlazor | UI Components |

## Fluxo de Autenticação

1. Usuário envia e-mail e senha para `POST /api/Usuario/login`
2. API valida credenciais e gera token JWT com claims (cpf, email, role)
3. Frontend armazena token no localStorage
4. Requisições subsequentes enviam token no header `Authorization: Bearer {token}`
5. API valida token e autoriza por role

## Regras de Negócio Críticas

- **Capacidade**: ingressos gerados na criação do evento — impossível vender além da capacidade
- **Anti-cambista**: um CPF só pode ter uma reserva por evento
- **Cupom seguro**: validação de valor mínimo e expiração antes de aplicar desconto
- **Bloqueio temporário**: ingresso fica com Status=1 durante o checkout, liberado automaticamente após expiração
