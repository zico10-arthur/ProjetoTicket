# 150 — Resiliência e Tratamento de Erros

> **Origem:** Pontos críticos [#7](../../sprints.md) e tarefas de sanitização do `sprints.md`
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Emitir e validar ingressos de maneira profissional: o sistema não pode quebrar com erro 500 genérico nem aceitar input malicioso.

---

## 150.1 O que resolve

| Item | Risco | Solução |
|------|-------|---------|
| IngressoController sem `[Authorize]` (#7) | Endpoint público que deveria ser protegido | Adicionar `[Authorize]` |
| Erros 500 genéricos | Usuário vê stack trace; dev não sabe o que aconteceu | GlobalExceptionHandlerMiddleware |
| Input sem sanitização | XSS, SQL Injection (mesmo com Dapper), campos com espaços | Trim + validação nos DTOs |
| Exceções não tratadas | Transação quebrada, estado inconsistente | Middleware captura tudo e retorna JSON padronizado |

---

## 150.2 Global Exception Handler

```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";

            (int statusCode, string message) = ex switch
            {
                CredenciaisInvalidasException => (401, "Email ou senha inválidos"),
                UnauthorizedException => (403, "Você não tem permissão para acessar este recurso"),
                NotFoundException => (404, ex.Message),
                ValidationException => (400, ex.Message),
                ConflitoException => (409, ex.Message),
                EventoJaComecouException => (400, "Não é possível cancelar. O evento já começou."),
                ReservaJaCanceladaException => (409, "Reserva já foi cancelada"),
                _ => (500, "Erro interno do servidor")
            };

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }
}
```

---

## 150.3 Registro no Program.cs

```csharp
var app = builder.Build();

// Middleware de exceção DEVE ser o primeiro
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## 150.4 Sanitização de Inputs

```csharp
public class CadastrarVendedorDTO
{
    private string _razaoSocial = "";

    public string RazaoSocial
    {
        get => _razaoSocial;
        set => _razaoSocial = value?.Trim() ?? "";
    }

    // Mesmo padrão para todos os campos string
    public string NomeFantasia { get; set; }   // set com Trim no service
    public string Email { get; set; }           // set com Trim + ToLower no service
}
```

### Regras globais

| Campo | Sanitização |
|-------|-------------|
| Strings | `.Trim()` em todos os campos de entrada |
| Email | `.Trim().ToLowerInvariant()` |
| CPF/CNPJ | Remove máscara: `Regex.Replace(cpf, "[^0-9]", "")` |
| Nome | `.Trim()`, máximo 200 caracteres |
| Descricao | `.Trim()`, máximo 2000 caracteres |

---

## 150.5 Controllers com Authorize Pendente

| Controller | Situação atual | Correção |
|-----------|---------------|----------|
| `IngressoController` | Sem `[Authorize]` | `[Authorize]` — todo acesso a ingressos requer autenticação |
| `ReservaController` | `[Authorize(Roles = "Comprador")]` | `[Authorize]` — Admin e Vendedor também podem (ST-07) |
| `CupomController` | AdminId via rota | `[Authorize(Roles = "Vendedor")]`, identidade do JWT |

---

## 150.6 Formato Padrão de Erro

Toda resposta de erro segue este formato:

```json
{
    "error": "Mensagem clara em português, sem detalhes internos"
}
```

| Código | Significado | Exemplo |
|--------|-------------|---------|
| `400` | Erro do cliente (validação) | `"CPF 12345678901 inválido"` |
| `401` | Não autenticado | `"Email ou senha inválidos"` |
| `403` | Não autorizado | `"Você não tem permissão para acessar este recurso"` |
| `404` | Não encontrado | `"Evento não encontrado"` |
| `409` | Conflito | `"CNPJ já cadastrado"` |
| `429` | Rate limit | `"Muitas tentativas. Aguarde 1 minuto."` |
| `500` | Erro interno | `"Erro interno do servidor"` (nunca expor stack trace) |
