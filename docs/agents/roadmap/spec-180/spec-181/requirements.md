# Spec 181: Correção de Bugs no Envio de E-mail (Spec 180) — Requirements

> **Projeto:** SoldOut Tickets
> **Status:** `audited`
> **Referências:** [Spec 180](../requirements.md) | [Spec 180 Design](../design.md) | [ADR-013](../../../ADR.md#adr-013-mailkit--channel-para-e-mails-transacionais-spec-180)

---

## Contexto

A Spec 180 (Serviço de E-mail Transacional + Redefinição de Senha) está `audited` e compila sem erros (0 warnings, 0 errors). No entanto, o envio de e-mail **não está funcionando em ambiente real**. A análise do código revelou **3 bugs** que, em conjunto, explicam a falha:

| ID | Severidade | Descrição | Impacto |
|----|-----------|-----------|---------|
| **BUG-001** | 🔴 Crítico | Links HTML quebrados nos templates de e-mail (escape incorreto de aspas em strings verbatim) | Links nos e-mails não são clicáveis; clientes de e-mail podem rejeitar o HTML malformado |
| **BUG-002** | 🟡 Alto | Log enganoso "E-mail enviado" quando SMTP não está configurado | Dificulta debugging — o dev vê "enviado" no log mas nenhum e-mail chega |
| **BUG-003** | 🟡 Médio | `Application` referencia diretamente `Infraestructure.Email.EmailTemplates` | Viola Clean Architecture (ADR-001); `Application.csproj` depende de `Infraestructure.csproj` |

---

## Diagnóstico Detalhado

### BUG-001: Links HTML Quebrados nos Templates

**Local:** `Infraestructure/Email/EmailTemplates.cs` — métodos `BoasVindasComprador`, `BoasVindasVendedor`, `RedefinicaoSenha`

**Causa raiz:** Uso incorreto de escape em strings verbatim interpoladas (`$@"..."`). Em strings verbatim C#, `""` é o escape para uma aspa dupla. O código atual usa `\""` que produz uma barra invertida literal seguida de aspa dupla (`\"`).

**Evidência (linha 14 do arquivo):**
```csharp
// ❌ ERRADO — produz: href=\"https://soldouttickets.com\"
<p>Acesse: <a href=\""https://soldouttickets.com\"">soldouttickets.com</a></p>

// ✅ CORRETO — produz: href="https://soldouttickets.com"
<p>Acesse: <a href=""https://soldouttickets.com"">soldouttickets.com</a></p>
```

**Consequência:** O HTML gerado contém `href=\"https://...\"` (com barras invertidas literais). Clientes de e-mail (Gmail, Outlook) interpretam isso como atributo `href` com valor `"https://...\` (com aspa no final), quebrando o link. Em casos extremos, o parser HTML do cliente de e-mail pode rejeitar toda a mensagem como malformada.

**Locais afetados (3 pontos no arquivo):**
- `BoasVindasComprador` — linha 14: link para `soldouttickets.com`
- `BoasVindasVendedor` — linha 28: link para `soldouttickets.com/painel`
- `RedefinicaoSenha` — linha 88: link de redefinição (`{link}`)

### BUG-002: Log Enganoso "E-mail enviado"

**Local:** `Infraestructure/Email/EmailBackgroundWorker.cs` — método `ProcessarComRetentativa`, linha 50

**Causa raiz:** O método `SmtpEmailSender.EnviarAsync` retorna sem lançar exceção quando `Configurado == false` (graceful degradation). O `ProcessarComRetentativa` interpreta qualquer retorno sem exceção como "sucesso" e loga `LogInformation("E-mail enviado para {Destinatario}: {Assunto}")`. 

**Fluxo do bug:**
1. Usuário faz cadastro → `EnfileirarAsync` enfileira no Channel ✅
2. `EmailBackgroundWorker` consome a mensagem → chama `ProcessarComRetentativa` ✅
3. `SmtpEmailSender.EnviarAsync` detecta `Configurado == false` → loga warning e retorna ✅
4. `ProcessarComRetentativa` vê retorno sem exceção → **loga "E-mail enviado"** ❌ (mentira!)
5. Dev olha o log, vê "E-mail enviado", mas nenhum e-mail chega → **confusão total**

### BUG-003: Violação da Clean Architecture

**Local:** `Application/Service/UsuarioService.cs` (linha 8), `ReservaService.cs` (linha 8), `PagamentoService.cs` (linha 6)

**Causa raiz:** A camada `Application` importa `using Infraestructure.Email;` para acessar diretamente a classe estática `EmailTemplates`. Isso cria uma dependência concreta da camada de aplicação na camada de infraestrutura, violando a regra da Clean Architecture (ADR-001) de que a camada interna (Application) não deve depender da camada externa (Infraestructure).

**Evidência:**
```csharp
// Application/Service/UsuarioService.cs:8
using Infraestructure.Email;  // ❌ Application depende de Infraestructure

// Uso direto da classe de infraestrutura:
var email = EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome);  // ❌
```

---

## Functional Requirements

### FR-001: Corrigir Escape de Aspas nos Templates HTML

**What:** Corrigir os 3 pontos em `EmailTemplates.cs` onde `\""` é usado incorretamente para escapar aspas dentro de strings verbatim. Substituir `\""` por `""` (escape correto em strings verbatim C#).

**Why:** Links quebrados impedem o usuário de clicar no link de redefinição de senha (BUG-001 crítico) e degradam a experiência em todos os e-mails transacionais.

**Acceptance Criteria:**
- [x] `BoasVindasComprador`: HTML gerado contém `href="https://soldouttickets.com"` (sem barras invertidas).
- [x] `BoasVindasVendedor`: HTML gerado contém `href="https://soldouttickets.com/painel"` (sem barras invertidas).
- [x] `RedefinicaoSenha`: HTML gerado contém `href="{link}"` com o link real, sem barras invertidas.
- [x] `dotnet build` compila sem erros.
- [x] Teste de unidade: verificar que o `CorpoHtml` de cada template contém `href="` (aspa dupla simples após `href=`).

### FR-002: Corrigir Log Enganoso no Worker

**What:** Modificar `EmailBackgroundWorker.ProcessarComRetentativa` para distinguir entre "e-mail enviado com sucesso" e "e-mail descartado (SMTP não configurado)". O `SmtpEmailSender.EnviarAsync` deve retornar um `bool` indicando se o envio realmente ocorreu.

**Why:** Sem essa distinção, é impossível diagnosticar por que e-mails não estão chegando (BUG-002). O desenvolvedor vê "E-mail enviado" no log e assume que o SMTP está funcionando.

**Acceptance Criteria:**
- [x] `SmtpEmailSender.EnviarAsync` agora retorna `Task<bool>` (`true` = enviou, `false` = descartou/SMTP não configurado).
- [x] `EmailBackgroundWorker.ProcessarComRetentativa` verifica o retorno:
  - `true` → loga `LogInformation("E-mail enviado para {Destinatario}: {Assunto}")`.
  - `false` → **NÃO loga** "enviado" (o warning já foi logado pelo `SmtpEmailSender`).
- [x] Se `Configurado == false`, o worker NÃO loga "E-mail enviado".
- [x] O contrato de `EnviarAsync` está documentado com comentário XML explicando o significado do retorno.
- [x] `dotnet build` compila sem erros.

### FR-003: Mover `EmailTemplates` para a Camada `Application`

**What:** Mover a classe `EmailTemplates` de `Infraestructure/Email/EmailTemplates.cs` para `Application/Email/EmailTemplates.cs`. Remover os `using Infraestructure.Email;` dos services da camada `Application`. Atualizar o namespace para `Application.Email`.

**Why:** Resolve a violação da Clean Architecture (BUG-003). A classe `EmailTemplates` é lógica de aplicação pura (constrói value objects de domínio com strings constantes) — não depende de MailKit, SMTP, ou qualquer biblioteca de infraestrutura. Seu lugar correto é na camada `Application`.

**Acceptance Criteria:**
- [x] Arquivo `Application/Email/EmailTemplates.cs` existe com o mesmo conteúdo (apenas namespace alterado para `Application.Email`).
- [x] Arquivo `Infraestructure/Email/EmailTemplates.cs` é **removido**.
- [x] Nenhum arquivo em `Application/` contém `using Infraestructure.Email;` (exceto se necessário para outros tipos como `SmtpSettings` — mas `SmtpSettings` não deve ser referenciado em `Application`).
- [x] `Application/Service/UsuarioService.cs`: substituir `using Infraestructure.Email;` por `using Application.Email;`.
- [x] `Application/Service/ReservaService.cs`: substituir `using Infraestructure.Email;` por `using Application.Email;`.
- [x] `Application/Service/PagamentoService.cs`: substituir `using Infraestructure.Email;` por `using Application.Email;`.
- [x] `dotnet build` compila sem erros em todos os projetos.

---

## Non-Functional Requirements

### NFR-001: Compatibilidade com Clientes de E-mail

**What:** O HTML gerado pelos templates deve ser compatível com os principais clientes de e-mail (Gmail, Outlook, Apple Mail). Após a correção do BUG-001, os atributos `href` devem ser bem formados (sem barras invertidas literais).

**Acceptance Criteria:**
- [x] `href` contém exatamente `="https://..."` (sem `\` antes das aspas).
- [x] HTML gerado passa em validador HTML básico (tags balanceadas, atributos bem formados).

### NFR-002: Observabilidade — Logs Precisos

**What:** Os logs do sistema de e-mail devem refletir precisamente o que aconteceu. Após a correção do BUG-002:
- "E-mail enviado" = o SMTP foi contatado e o e-mail foi aceito.
- "SMTP não configurado. E-mail descartado" = o e-mail NÃO foi enviado.
- NUNCA as duas mensagens para o mesmo e-mail.

**Acceptance Criteria:**
- [x] Nenhum log de "E-mail enviado" aparece se `Configurado == false`.
- [x] Logs de warning e error usam interpolação estruturada (`{Destinatario}`), não concatenação.

---

## Constraints

- **C1:** Não alterar a interface `IEmailSender` no Domain — a mudança de `EnviarAsync` para retornar `bool` afeta apenas `SmtpEmailSender` (classe concreta), não a interface.
- **C2:** `EmailTemplates` é uma classe estática pura — não requer injeção de dependência. Pode residir em `Application` sem violar princípios.
- **C3:** A correção do BUG-001 não deve alterar o conteúdo semântico dos templates (apenas corrigir o escape).
- **C4:** Manter compatibilidade com .NET 9.
- **C5:** Não alterar o comportamento de graceful degradation — sistema continua operando sem SMTP configurado.

---

## Edge Cases

| # | Caso | Comportamento Esperado |
|---|------|------------------------|
| **E1** | Template com nome contendo caracteres HTML (`<script>`) | `HtmlEncode` já está aplicado. A correção de escape não afeta essa proteção. |
| **E2** | Link de redefinição contendo `&`, `=`, `?` | `Uri.EscapeDataString` já é aplicado no token. A correção de escape mantém o link funcional. |
| **E3** | SMTP configurado mas offline | `EnviarAsync` lança exceção → retry 3x → `LogError`. Não há mudança de comportamento. |
| **E4** | SMTP configurado e funcional | `EnviarAsync` retorna `true`. Worker loga "E-mail enviado". Comportamento igual ao anterior, mas com `bool` explícito. |

---

## Dependencies

### Internal
- **Spec 180:** Esta spec corrige bugs da implementação da Spec 180. Nenhuma mudança estrutural — apenas correções pontuais.
- **ADR-001 (Clean Architecture):** A correção do BUG-003 alinha o código com a arquitetura definida.

### External
- Nenhuma nova dependência externa.

---

## Out of Scope

- Adicionar suporte a user-secrets para configuração SMTP (já funciona via `IOptions<T>` padrão).
- Migrar templates para Razor/Liquid.
- Implementar fila persistente (RabbitMQ, Azure Service Bus).
- Adicionar dashboard de monitoramento de e-mails.
- Corrigir outros bugs não listados neste documento.
