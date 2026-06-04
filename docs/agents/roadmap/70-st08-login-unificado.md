# 70 — ST-08: Login Unificado — Três Perfis, Um Endpoint

> **Origem:** [`storytelling.md#st-08-login-unificado`](../../storytelling.md#st-08-login-unificado)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Ter autonomia: experiência de autenticação simplificada, sem endpoints diferentes por perfil.

---

## 70.1 História

**Como** qualquer usuário (Admin, Vendedor ou Comprador),
**Quero** fazer login no mesmo endpoint,
**Para** ter uma experiência de autenticação simplificada.

---

## 70.2 Endpoint

```
POST /api/usuario/login
Auth: Público
Content-Type: application/json
```

### Request (único para todos os perfis)

```json
{
    "email": "admin@soldout.com",
    "senha": "Admin@123"
}
```

### Response (role varia conforme PerfilId)

```json
{
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "usuario": {
        "cpf": "00000000000",
        "nome": "Administrador",
        "email": "admin@soldout.com",
        "perfil": "Admin"
    }
}
```

---

## 70.3 Lógica de Login

```csharp
public LoginResponseDTO Login(LoginDTO dto)
{
    // 80.3.1 Buscar usuário por email (tabela única)
    var usuario = _usuarioRepo.BuscarPorEmail(dto.Email);
    if (usuario == null)
        throw new CredenciaisInvalidasException();

    // 80.3.2 Verificar senha com BCrypt
    if (!BCrypt.Verify(dto.Senha, usuario.Senha))
        throw new CredenciaisInvalidasException();

    // 80.3.3 Verificar se usuário está ativo
    if (!usuario.Ativo)
        throw new UsuarioInativoException();

    // 80.3.4 Obter nome do perfil
    var perfil = _perfilRepo.BuscarPorId(usuario.PerfilId);

    // 80.3.5 Gerar JWT com role do perfil
    var token = _tokenService.GerarToken(
        cpf: usuario.Cpf,
        email: usuario.Email,
        role: perfil.Nome  // "Admin", "Vendedor" ou "Comprador"
    );

    return new LoginResponseDTO { Token = token, ... };
}
```

---

## 70.4 Geração do JWT

```csharp
public string GerarToken(string cpf, string email, string role)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, cpf),  // sub
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, role)             // "Admin" | "Vendedor" | "Comprador"
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

## 70.5 Mudança Estrutural

| Item | Antes | Agora |
|------|-------|-------|
| Login Vendedor | `POST /api/empresa/login` | `POST /api/usuario/login` |
| Tabela Vendedor | `Empresas` (separada) | `Usuarios` (colunas específicas) |
| Role no JWT | `"Empresa"` | `"Vendedor"` |
| Cadastro Vendedor | Admin cadastrava | Auto cadastro público |

## 70.6 Respostas

| Código | Caso |
|--------|------|
| `200 OK` | Login bem-sucedido, retorna JWT |
| `401 Unauthorized` | Email ou senha inválidos |
| `403 Forbidden` | Usuário inativo (Ativo = false) |
