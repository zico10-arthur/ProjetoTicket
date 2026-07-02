# Spec 181: Correção de Bugs no Envio de E-mail (Spec 180) — Design

> **Requirements:** [`requirements.md`](./requirements.md)
> **Spec Base:** [Spec 180 Design](../design.md)
> **Arquitetura:** [Clean Architecture](../../../arquitetura.md) | [ADR-001](../../../ADR.md#adr-001-clean-architecture-com-4-camadas)

---

## Design Approach

**High-level strategy:** Três correções cirúrgicas e independentes — cada bug resolvido com alteração mínima e focada. Nenhuma mudança arquitetural profunda; apenas alinhamento com o design original da Spec 180 e com a Clean Architecture.

**Princípios aplicados:**
- **Surgical Changes (Karpathy #3):** Cada correção toca apenas o necessário. Sem refatorações "de bônus".
- **Single Responsibility:** `EmailTemplates` é lógica de aplicação pura → camada `Application`.
- **Observabilidade:** Logs devem refletir a verdade — `bool` de retorno elimina ambiguidade.

---

## Fix 1: Corrigir Escape de Aspas nos Templates (BUG-001)

### Arquivo: `Infraestructure/Email/EmailTemplates.cs` → `Application/Email/EmailTemplates.cs`

**Problema:** Em strings verbatim C# (`$@"..."`), o escape para aspa dupla é `""` (duas aspas). O código atual usa `\""` que produz `\"` (barra invertida literal + aspa).

**Solução:** Substituir `\""` por `""` nos 3 pontos afetados.

| Método | Linha | String Atual (errada) | String Corrigida |
|--------|-------|----------------------|-------------------|
| `BoasVindasComprador` | 14 | `href=\""https://soldouttickets.com\""` | `href=""https://soldouttickets.com""` |
| `BoasVindasVendedor` | 28 | `href=\""https://soldouttickets.com/painel\""` | `href=""https://soldouttickets.com/painel""` |
| `RedefinicaoSenha` | 88 | `href=\""{link}\""` | `href=""{link}""` |

**Explicação técnica:** Em uma string verbatim `@"..."`, a sequência `""` é interpretada pelo compilador C# como uma aspa dupla literal. A sequência `\"` é interpretada como dois caracteres literais: barra invertida + aspa dupla. Como a string é usada como HTML, o browser/cliente de e-mail recebe `\"` e interpreta como barra invertida literal, quebrando o atributo `href`.

**Antes (BUG):**
```csharp
var html = $@"
<h1>Bem-vindo, {System.Net.WebUtility.HtmlEncode(nome)}!</h1>
<p>Acesse: <a href=\""https://soldouttickets.com\"">soldouttickets.com</a></p>
<p>— Equipe SoldOut Tickets</p>";
// HTML gerado: <a href=\"https://soldouttickets.com\">  ← QUEBRADO!
```

**Depois (FIX):**
```csharp
var html = $@"
<h1>Bem-vindo, {System.Net.WebUtility.HtmlEncode(nome)}!</h1>
<p>Acesse: <a href=""https://soldouttickets.com"">soldouttickets.com</a></p>
<p>— Equipe SoldOut Tickets</p>";
// HTML gerado: <a href="https://soldouttickets.com">  ← CORRETO!
```

---

## Fix 2: Log Preciso no Worker (BUG-002)

### Arquivos afetados:
- `Infraestructure/Email/SmtpEmailSender.cs` — alterar assinatura de `EnviarAsync`
- `Infraestructure/Email/EmailBackgroundWorker.cs` — verificar retorno antes de logar

### Mudança em `SmtpEmailSender`

**Antes:**
```csharp
public async Task EnviarAsync(EmailMessage email, CancellationToken ct)
{
    if (!Configurado)
    {
        _logger.LogWarning("SMTP não configurado. E-mail descartado para {Destinatario}", email.Destinatario);
        return;  // Retorna void — caller não sabe se enviou ou descartou
    }
    // ... envio SMTP ...
}
```

**Depois:**
```csharp
/// <summary>
/// Envia o e-mail via SMTP.
/// </summary>
/// <returns>true se o e-mail foi enviado com sucesso; false se foi descartado (SMTP não configurado).</returns>
public async Task<bool> EnviarAsync(EmailMessage email, CancellationToken ct)
{
    if (!Configurado)
    {
        _logger.LogWarning("SMTP não configurado. E-mail descartado para {Destinatario}", email.Destinatario);
        return false;  // ← Sinaliza que NÃO enviou
    }
    // ... envio SMTP (inalterado) ...
    await client.DisconnectAsync(true, ct);
    return true;  // ← Sinaliza que enviou com sucesso
}
```

### Mudança em `EmailBackgroundWorker.ProcessarComRetentativa`

**Antes:**
```csharp
await sender.EnviarAsync(email, ct);
_logger.LogInformation("E-mail enviado para {Destinatario}: {Assunto}",
    email.Destinatario, email.Assunto);
return; // Sucesso
```

**Depois:**
```csharp
var enviado = await sender.EnviarAsync(email, ct);
if (enviado)
{
    _logger.LogInformation("E-mail enviado para {Destinatario}: {Assunto}",
        email.Destinatario, email.Assunto);
}
// Se !enviado, o SmtpEmailSender já logou o warning — não logar "enviado" falso
return;
```

**Nota:** Se `EnviarAsync` lançar exceção (ex: servidor offline), o `catch` no loop de retry é acionado normalmente. O `bool` só é relevante para o caso de graceful degradation (SMTP não configurado).

---

## Fix 3: Mover EmailTemplates para Application (BUG-003)

### Arquivos afetados:

| Ação | Arquivo |
|------|---------|
| **Mover** | `Infraestructure/Email/EmailTemplates.cs` → `Application/Email/EmailTemplates.cs` |
| **Editar** | `Application/Service/UsuarioService.cs` — trocar `using` |
| **Editar** | `Application/Service/ReservaService.cs` — trocar `using` |
| **Editar** | `Application/Service/PagamentoService.cs` — trocar `using` |

### Nova localização

```
Application/
  Email/
    EmailTemplates.cs    ← MOVIDO de Infraestructure/Email/
```

**Namespace:** `Application.Email` (antes: `Infraestructure.Email`)

**Justificativa técnica:** `EmailTemplates` é uma classe estática pura que:
1. **Não depende de MailKit, MimeKit, ou SMTP** — apenas de `Domain.ValueObjects.EmailMessage`.
2. **Contém lógica de aplicação** — construir value objects de domínio a partir de templates.
3. **Não requer injeção de dependência** — métodos estáticos, sem estado.

Segundo a Clean Architecture (ADR-001), a camada `Application` pode depender de `Domain`, mas NÃO de `Infraestructure`. Mover `EmailTemplates` para `Application` resolve a violação sem introduzir novas dependências.

### Mudança nos Services

**Antes (3 services):**
```csharp
using Infraestructure.Email;  // ❌ Application depende de Infraestructure

var email = EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome);
```

**Depois (3 services):**
```csharp
using Application.Email;  // ✅ Application depende de Application (mesma camada)

var email = EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome);
```

### Verificação pós-move

O projeto `Infraestructure` NÃO referenciava `EmailTemplates` internamente (apenas `SmtpEmailSender` e `EmailBackgroundWorker` usam `EmailMessage`, não templates). Nenhum ajuste necessário no `Infraestructure`.

---

## Impact Analysis

| Componente | Fix 1 (HTML) | Fix 2 (Log) | Fix 3 (Move) |
|------------|:-----------:|:-----------:|:------------:|
| Mudança de API pública? | Não | Sim (`EnviarAsync` retorna `bool`) | Não (mesma assinatura) |
| Quebra compatibilidade binária? | Não | Sim (método interno) | Não |
| Afeta testes? | Sim (verificar HTML) | Sim (mock retorno) | Não (mesmo comportamento) |
| Risco de regressão? | Nenhum | Baixo | Nenhum |

---

## Testing Strategy

### Testes Existentes Afetados

Nenhum teste existente quebra — os testes da Spec 180 mockam `IEmailSender` e `SmtpEmailSender`, e a mudança de `void` para `bool` no `EnviarAsync` é interna (não afeta a interface `IEmailSender`).

### Novos Testes

| # | Teste | O que verifica | Bug |
|---|-------|---------------|-----|
| T1 | `BoasVindasComprador_HtmlContemHrefCorreto` | `CorpoHtml` contém `href="https://` (sem `\"`) | BUG-001 |
| T2 | `BoasVindasVendedor_HtmlContemHrefCorreto` | `CorpoHtml` contém `href="https://` (sem `\"`) | BUG-001 |
| T3 | `RedefinicaoSenha_HtmlContemHrefCorreto` | `CorpoHtml` contém `href="{link}"` (sem `\"`) | BUG-001 |
| T4 | `SmtpEmailSender_EnviarAsync_RetornaTrue_QuandoConfigurado` | Mock SMTP → `EnviarAsync` retorna `true` | BUG-002 |
| T5 | `SmtpEmailSender_EnviarAsync_RetornaFalse_QuandoNaoConfigurado` | `Host=""` → `EnviarAsync` retorna `false` | BUG-002 |
| T6 | `EmailTemplates_Namespace_ApplicationEmail` | `typeof(EmailTemplates).Namespace == "Application.Email"` | BUG-003 |

---

## Rollback

Cada fix é independente e reversível isoladamente:

1. **Fix 1:** Reverter `""` para `\""` nos 3 pontos (git revert).
2. **Fix 2:** Reverter `Task<bool>` para `Task` e remover verificação `if (enviado)`.
3. **Fix 3:** Mover arquivo de volta para `Infraestructure/Email/`, reverter `using` nos 3 services.

Nenhum fix requer migração de banco de dados ou alteração de configuração.
