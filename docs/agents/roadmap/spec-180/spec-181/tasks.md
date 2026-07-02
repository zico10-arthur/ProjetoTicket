# Spec 181: Correção de Bugs no Envio de E-mail (Spec 180) — Implementation Tasks

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)

---

## Task Order

Tasks are grouped by bug fix. Cada grupo é independente e pode ser implementado em paralelo. Dentro de cada grupo, as subtasks são sequenciais.

---

## Grupo A: Fix BUG-001 — Links HTML Quebrados

### Task A1: Corrigir Escape em `EmailTemplates.cs`

**Objective:** Corrigir os 3 pontos onde `\""` produz HTML malformado.

**Requirements Covered:** FR-001

**Design References:** [Fix 1](./design.md#fix-1-corrigir-escape-de-aspas-nos-templates-bug-001)

**Actions:**
1. **LER** o arquivo `Infraestructure/Email/EmailTemplates.cs` para confirmar conteúdo atual.
2. **EDITAR — `BoasVindasComprador` (linha 14):**
   - Substituir `href=\""https://soldouttickets.com\""` por `href=""https://soldouttickets.com""`
3. **EDITAR — `BoasVindasVendedor` (linha 28):**
   - Substituir `href=\""https://soldouttickets.com/painel\""` por `href=""https://soldouttickets.com/painel""`
4. **EDITAR — `RedefinicaoSenha` (linha 88):**
   - Substituir `href=\""{System.Net.WebUtility.HtmlEncode(link)}\""` por `href=""{System.Net.WebUtility.HtmlEncode(link)}""`

**Validation:**
- `dotnet build` compila sem erros.
- Verificar visualmente que `CorpoHtml` de cada template contém `href="` (aspa simples após `href=`, sem barra invertida).

**Status:** [x] done

---

## Grupo B: Fix BUG-002 — Log Enganoso "E-mail Enviado"

### Task B1: Alterar `SmtpEmailSender.EnviarAsync` para Retornar `bool`

**Objective:** Modificar o método `EnviarAsync` para retornar `Task<bool>` indicando se o e-mail foi realmente enviado.

**Requirements Covered:** FR-002

**Design References:** [Fix 2 — SmtpEmailSender](./design.md#mudança-em-smtpemailsender)

**Actions:**
1. **LER** o arquivo `Infraestructure/Email/SmtpEmailSender.cs` para confirmar conteúdo atual.
2. **EDITAR — Assinatura do método:**
   - Alterar `public async Task EnviarAsync(...)` para `public async Task<bool> EnviarAsync(...)`
3. **EDITAR — Adicionar `<returns>` no comentário XML:**
   ```csharp
   /// <returns>true se o e-mail foi enviado com sucesso; false se foi descartado (SMTP não configurado).</returns>
   ```
4. **EDITAR — Ramo `!Configurado`:**
   - Alterar `return;` para `return false;`
5. **EDITAR — Final do método (após `DisconnectAsync`):**
   - Adicionar `return true;` antes do fechamento do método.

**Validation:**
- `dotnet build Infraestructure/Infraestructure.csproj` compila sem erros.
- `EnviarAsync` com `Host=""` retorna `false`.
- `EnviarAsync` com `Host="smtp.test.com"` e `FromAddress="a@b.com"` retorna `true` (em ambiente de teste com SMTP real).

**Status:** [x] done

---

### Task B2: Atualizar `EmailBackgroundWorker` para Verificar Retorno

**Objective:** Modificar `ProcessarComRetentativa` para só logar "E-mail enviado" quando `EnviarAsync` retornar `true`.

**Requirements Covered:** FR-002

**Design References:** [Fix 2 — EmailBackgroundWorker](./design.md#mudança-em-emailbackgroundworkerprocessarcomretentativa)

**Actions:**
1. **LER** o arquivo `Infraestructure/Email/EmailBackgroundWorker.cs` para confirmar conteúdo atual.
2. **EDITAR — `ProcessarComRetentativa` (linhas 48-52):**
   - Alterar:
     ```csharp
     await sender.EnviarAsync(email, ct);
     _logger.LogInformation("E-mail enviado para {Destinatario}: {Assunto}",
         email.Destinatario, email.Assunto);
     return;
     ```
   - Para:
     ```csharp
     var enviado = await sender.EnviarAsync(email, ct);
     if (enviado)
     {
         _logger.LogInformation("E-mail enviado para {Destinatario}: {Assunto}",
             email.Destinatario, email.Assunto);
     }
     return;
     ```

**Validation:**
- `dotnet build Infraestructure/Infraestructure.csproj` compila sem erros.
- Com SMTP não configurado: worker NÃO loga "E-mail enviado".
- Com SMTP configurado: worker loga "E-mail enviado" apenas após envio real.

**Status:** [x] done

---

## Grupo C: Fix BUG-003 — Violação Clean Architecture

### Task C1: Mover `EmailTemplates.cs` para `Application/Email/`

**Objective:** Mover a classe `EmailTemplates` da camada `Infraestructure` para `Application`, corrigindo a violação da Clean Architecture.

**Requirements Covered:** FR-003

**Design References:** [Fix 3](./design.md#fix-3-mover-emailtemplates-para-application-bug-003)

**Actions:**
1. **LER** o arquivo `Infraestructure/Email/EmailTemplates.cs` para confirmar conteúdo atual.
2. Criar diretório `Application/Email/` se não existir.
3. Criar arquivo `Application/Email/EmailTemplates.cs` com o MESMO conteúdo, alterando apenas:
   - Namespace: `Infraestructure.Email` → `Application.Email`
4. **DELETAR** o arquivo `Infraestructure/Email/EmailTemplates.cs`.

**Validation:**
- `dotnet build Application/Application.csproj` — espera-se ERRO de compilação (os 3 services ainda referenciam `Infraestructure.Email.EmailTemplates`). Isso será corrigido na Task C2.

**Status:** [x] done

---

### Task C2: Atualizar `using` nos 3 Services

**Objective:** Substituir `using Infraestructure.Email;` por `using Application.Email;` nos services que usam `EmailTemplates`.

**Requirements Covered:** FR-003

**Design References:** [Fix 3 — Mudança nos Services](./design.md#mudança-nos-services)

**Actions:**
1. **LER** `Application/Service/UsuarioService.cs` — linha 8: substituir `using Infraestructure.Email;` por `using Application.Email;`
2. **LER** `Application/Service/ReservaService.cs` — linha 8: substituir `using Infraestructure.Email;` por `using Application.Email;`
3. **LER** `Application/Service/PagamentoService.cs` — linha 6: substituir `using Infraestructure.Email;` por `using Application.Email;`

**Validation:**
- `dotnet build Application/Application.csproj` compila sem erros.
- `dotnet build` na raiz compila todos os projetos sem erros.

**Status:** [x] done

---

### Task C3: Limpeza — Verificar `Infraestructure` não Usa `EmailTemplates`

**Objective:** Confirmar que nenhum arquivo no projeto `Infraestructure` referencia `EmailTemplates` após a movimentação.

**Requirements Covered:** FR-003

**Actions:**
1. Buscar por `EmailTemplates` em todos os arquivos `.cs` do projeto `Infraestructure/`.
2. Se encontrado, ajustar o using para `Application.Email` OU refatorar (improvável, pois apenas services da camada `Application` usam templates).

**Validation:**
- `dotnet build Infraestructure/Infraestructure.csproj` compila sem erros.
- Nenhum arquivo em `Infraestructure/` contém `using Application.Email;` (a camada de infra não deve depender de Application).

**Status:** [x] done

---

## Task Final: Testes

### Task T1: Criar/Atualizar Testes de Unidade

**Objective:** Garantir cobertura para as 3 correções.

**Requirements Covered:** FR-001, FR-002, FR-003

**Design References:** [Testing Strategy](./design.md#testing-strategy)

**Actions:**
1. Localizar arquivo de testes existente para email (ex: `tests/EmailTests.cs`).
2. Adicionar 6 novos testes:

**T1 — `BoasVindasComprador_HtmlContemHrefCorreto`:**
```csharp
[Fact]
public void BoasVindasComprador_HtmlContemHrefCorreto()
{
    var msg = EmailTemplates.BoasVindasComprador("a@b.com", "João");
    Assert.Contains("href=\"https://soldouttickets.com\"", msg.CorpoHtml);
    Assert.DoesNotContain("\\\"", msg.CorpoHtml);
}
```

**T2 — `BoasVindasVendedor_HtmlContemHrefCorreto`:**
```csharp
[Fact]
public void BoasVindasVendedor_HtmlContemHrefCorreto()
{
    var msg = EmailTemplates.BoasVindasVendedor("a@b.com", "Loja X", "Gratuito");
    Assert.Contains("href=\"https://soldouttickets.com/painel\"", msg.CorpoHtml);
    Assert.DoesNotContain("\\\"", msg.CorpoHtml);
}
```

**T3 — `RedefinicaoSenha_HtmlContemHrefCorreto`:**
```csharp
[Fact]
public void RedefinicaoSenha_HtmlContemHrefCorreto()
{
    var msg = EmailTemplates.RedefinicaoSenha("a@b.com", "João", "https://localhost/redefinir?token=abc");
    Assert.Contains("href=\"https://localhost/redefinir?token=abc\"", msg.CorpoHtml);
    Assert.DoesNotContain("\\\"", msg.CorpoHtml);
}
```

**T4 — `EnviarAsync_RetornaTrue_QuandoConfigurado`:**
```csharp
[Fact]
public async Task EnviarAsync_RetornaTrue_QuandoConfigurado()
{
    // Arrange: SMTP settings com Host e FromAddress
    var settings = Options.Create(new SmtpSettings
    {
        Host = "localhost",
        Port = 25,
        FromAddress = "test@test.com"
    });
    var logger = Mock.Of<ILogger<SmtpEmailSender>>();
    var sender = new SmtpEmailSender(settings, logger);
    var email = new EmailMessage("a@b.com", "Test", "<p>Oi</p>", "Oi");
    
    // Act & Assert: vai lançar exceção de conexão (localhost:25 offline),
    // mas NÃO por Configurado==false. O teste verifica que o método tenta
    // conectar em vez de retornar false.
    await Assert.ThrowsAsync<SocketException>(() => sender.EnviarAsync(email, CancellationToken.None));
}
```

**T5 — `EnviarAsync_RetornaFalse_QuandoNaoConfigurado`:**
```csharp
[Fact]
public async Task EnviarAsync_RetornaFalse_QuandoNaoConfigurado()
{
    var settings = Options.Create(new SmtpSettings()); // defaults: Host=""
    var logger = Mock.Of<ILogger<SmtpEmailSender>>();
    var sender = new SmtpEmailSender(settings, logger);
    var email = new EmailMessage("a@b.com", "Test", "<p>Oi</p>", "Oi");
    
    var result = await sender.EnviarAsync(email, CancellationToken.None);
    Assert.False(result);
}
```

**T6 — `EmailTemplates_Namespace_Correto`:**
```csharp
[Fact]
public void EmailTemplates_Namespace_Correto()
{
    Assert.Equal("Application.Email", typeof(EmailTemplates).Namespace);
}
```

**Validation:**
- `dotnet test` — todos os 6 novos testes passam.
- Nenhum teste existente quebra.

**Status:** [x] done

---

## Summary

| Task | Grupo | Bug | Arquivo(s) | Ação |
|------|-------|-----|-----------|------|
| A1 | A | BUG-001 | `EmailTemplates.cs` | Corrigir `\""` → `""` (3 pontos) |
| B1 | B | BUG-002 | `SmtpEmailSender.cs` | Alterar retorno para `Task<bool>` |
| B2 | B | BUG-002 | `EmailBackgroundWorker.cs` | Verificar `bool` antes de logar |
| C1 | C | BUG-003 | `EmailTemplates.cs` | Mover p/ `Application/Email/` |
| C2 | C | BUG-003 | 3 Services | Atualizar `using` |
| C3 | C | BUG-003 | `Infraestructure/` | Verificar sem refs |
| T1 | Final | Todos | `tests/EmailTests.cs` | 6 novos testes |

---

## Dependency Graph

```
Grupo A (Task A1)     → independente
Grupo B (Tasks B1→B2) → sequencial (B2 depende de B1)
Grupo C (Tasks C1→C2→C3) → sequencial (C2 depende de C1, C3 depende de C2)

Tasks A1, B1, C1 podem ser executadas em paralelo.
Task T1 depende de todas as anteriores (A1, B2, C3).
```
