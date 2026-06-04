# 10 — ST-01: Auto Cadastro de Vendedor

> **Origem:** [`storytelling.md#st-01-auto-cadastro-de-vendedor`](../../storytelling.md#st-01-auto-cadastro-de-vendedor)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Ter autonomia para se cadastrar e começar a vender imediatamente, sem depender de um administrador.

---

## 10.1 História

**Como** uma empresa que deseja vender ingressos para eventos de pequeno porte,
**Quero** me cadastrar diretamente no site, sem depender de um administrador,
**Para** começar a criar e vender eventos imediatamente.

---

## 10.2 Endpoint

```
POST /api/usuario/cadastrar-vendedor
Auth: Público (sem token)
Content-Type: application/json
```

## 10.3 Request Body

```json
{
    "cnpj": "11222333000181",
    "razaoSocial": "Workshop de Tecnologia Ltda",
    "nomeFantasia": "Tech Workshops",
    "email": "contato@techworkshops.com.br",
    "senha": "Senha@123",
    "telefone": "(11) 99999-0001"
}
```

## 10.4 Validações

| Campo | Regra | Erro |
|-------|-------|------|
| `cnpj` | 14 dígitos, dígitos verificadores válidos, único na tabela | 400 "CNPJ inválido" / 409 "CNPJ já cadastrado" |
| `razaoSocial` | Obrigatório, não vazio | 400 "Razão Social é obrigatória" |
| `nomeFantasia` | Obrigatório, não vazio | 400 "Nome Fantasia é obrigatório" |
| `email` | Formato válido, único entre TODOS os usuários (Admin, Vendedor, Comprador) | 400 "E-mail inválido" / 409 "E-mail já cadastrado" |
| `senha` | Mínimo 8 caracteres, 1 letra, 1 número, 1 caractere especial | 400 "Senha não atende aos requisitos" |
| `telefone` | Opcional, formato (XX) XXXXX-XXXX | 400 "Telefone inválido" |

## 10.5 Regras de Negócio

```
1. Validar CNPJ (CnpjValidator.Validar):
   ├── Remove máscara
   ├── Verifica 14 dígitos
   ├── Calcula dígitos verificadores (pesos oficiais)
   └── Inválido → 400

2. Verificar unicidade:
   ├── SELECT COUNT(*) FROM Usuarios WHERE Cnpj = @cnpj → > 0 → 409
   └── SELECT COUNT(*) FROM Usuarios WHERE Email = @email → > 0 → 409

3. Criar usuário:
   ├── BCrypt.HashPassword(senha) → hash
   ├── PerfilId = B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2 (Vendedor)
   ├── Cpf = CNPJ (campo usado como identificador único)
   ├── Plano = 0 (Gratuito)
   └── INSERT INTO Usuarios (...)
```

## 10.6 Response

| Código | Caso |
|--------|------|
| `201 Created` | Vendedor cadastrado com sucesso |
| `400 Bad Request` | Dados inválidos (CNPJ, email, senha, campos obrigatórios) |
| `409 Conflict` | CNPJ ou email já cadastrado |

```json
// 201 Created
{
    "cpf": "11222333000181",
    "nome": "Workshop de Tecnologia Ltda",
    "nomeFantasia": "Tech Workshops",
    "email": "contato@techworkshops.com.br",
    "perfil": "Vendedor",
    "plano": "Gratuito"
}
```

## 10.7 SQL

```sql
INSERT INTO Usuarios (Cpf, Nome, NomeFantasia, Email, Senha, PerfilId,
                       Cnpj, Telefone, Plano, Ativo, DataCriacao)
VALUES (@cpf, @nome, @nomeFantasia, @email, @senhaHash,
        'B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2',
        @cnpj, @telefone, 0, 1, GETDATE());
```
