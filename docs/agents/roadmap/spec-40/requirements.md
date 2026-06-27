# Spec 40: Comprador Cancela Reserva com Reembolso — Requirements

> **Projeto:** SoldOut Tickets
> **Status:** `audited`
> **Referências:** [`visao.md §6.5`](../../../visao.md#65-cancelamento-com-reembolso-atômico) | [`arquitetura.md §4.1`](../../../arquitetura.md#41-comprador-cancela-reserva-spec-40) | [`ADR-012`](../../../ADR.md#adr-012-cancelamento-lógico-com-reembolso-atômico-specs-40-50-110) | [`storytelling.md#st-05`](../../../storytelling.md#st-05-comprador-cancela-reserva-com-reembolso) | [`sprints.md §ST-05`](../../../sprints.md#st-05--cancelamento-de-reserva-com-reembolso-)

---

## Value Delivery

Do [`visao.md §6.5`](../../../visao.md#65-cancelamento-com-reembolso-atômico):

> **Flexibilidade com segurança:** o comprador não perde dinheiro se não puder comparecer, desde que cancele antes do evento começar. As vagas voltam para outros compradores.

Do [`storytelling.md#st-05`](../../../storytelling.md#st-05-comprador-cancela-reserva-com-reembolso):

> **Como** um comprador (ou Admin, ou Vendedor),
> **Quero** cancelar minha reserva antes do evento começar e receber reembolso,
> **Para** não perder dinheiro se não puder mais comparecer.

Adicionalmente do [`visao.md §6.9`](../../../visao.md#69-e-mails-transacionais-e-redefinição-de-senha):

> **Tranquilidade financeira:** o comprador tem a garantia documentada de que o reembolso foi processado.

---

## Functional Requirements

### FR-001: Endpoint `DELETE /api/reserva/{id}`

**What:** O sistema deve expor o endpoint `DELETE /api/reserva/{id}` no `ReservaController`, protegido por `[Authorize]` (qualquer perfil autenticado: Comprador, Vendedor, Admin). O endpoint extrai o CPF do usuário autenticado da claim `"cpf"` do JWT (`User.Claims`), nunca de parâmetro de rota ou body. Chama `IReservaService.CancelarReserva(reservaId, cpfUsuarioLogado, ct)`. Em caso de sucesso, retorna `200 OK` com body `{ "message": "Reserva cancelada com sucesso. Reembolso registrado." }`.

**Why:** Permite que o dono da reserva cancele-a. Extrair CPF do JWT impede falsificação de identidade (ADR-004, ADR-012).

**Acceptance Criteria:**
- [ ] `ReservaController` contém método `CancelarReserva` com `[HttpDelete("{id:guid}")]` e `[Authorize]`.
- [ ] CPF extraído de `User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value`.
- [ ] Se CPF não encontrado no JWT: retorna `401 Unauthorized` com `{ "message": "CPF não encontrado no token." }`.
- [ ] `Swagger` exibe `DELETE /api/reserva/{id}` como operação disponível.

### FR-002: Autorização — Apenas o Dono Cancela

**What:** O sistema deve rejeitar o cancelamento se `Reserva.UsuarioCpf != cpfDoJWT`, retornando `403 Forbidden` com `{ "message": "Esta reserva não pertence a você." }`. A validação ocorre no service (`ReservaService.CancelarReserva`), não no controller.

**Why:** Isolamento de segurança — um usuário não pode cancelar reservas de terceiros. Admin tem endpoints separados (spec 110) para gestão de cancelamentos.

**Acceptance Criteria:**
- [ ] Se `Reserva.UsuarioCpf != cpfDoJWT`: lança `UnauthorizedAccessException` com mensagem "Esta reserva não pertence a você."
- [ ] Controller captura `UnauthorizedAccessException` e retorna `403 Forbidden`.
- [ ] Se `Reserva.UsuarioCpf == cpfDoJWT`: a validação passa sem exceção.

### FR-003: Cancelamento Bloqueado se Evento Já Começou

**What:** O sistema deve rejeitar o cancelamento se `DataEvento <= DateTime.UtcNow` (evento já iniciou ou passou), retornando `400 Bad Request` com `{ "message": "Não é possível cancelar. O evento já começou." }`. O `DateTime.UtcNow` é calculado no momento da validação, no service.

**Why:** Um comprador não deve poder cancelar e obter reembolso depois que o evento começou (evita fraude: assistir ao evento e depois cancelar).

**Acceptance Criteria:**
- [ ] Se `evento.DataEvento <= DateTime.UtcNow`: lança `DomainException` com mensagem "Não é possível cancelar. O evento já começou."
- [ ] Se `evento.DataEvento > DateTime.UtcNow`: a validação passa (cancelamento permitido).
- [ ] A comparação usa `DateTime.UtcNow` (UTC), consistente com o resto do projeto.

### FR-004: Cancelamento Bloqueado se Reserva Já Reembolsada

**What:** O sistema deve rejeitar o cancelamento se `Reserva.Reembolsada == true` (já foi cancelada anteriormente), retornando `409 Conflict` com `{ "message": "Reserva já foi cancelada." }`. A validação ocorre no service.

**Why:** Idempotência — impedir duplo cancelamento que causaria duplo reembolso e inconsistência de estado.

**Acceptance Criteria:**
- [ ] Se `reserva.Reembolsada == true`: lança `DomainException` com mensagem "Reserva já foi cancelada."
- [ ] Controller captura `DomainException` com essa mensagem específica e retorna `409 Conflict`.
- [ ] Se `reserva.Reembolsada == false`: a validação passa.

### FR-005: Marcar Reserva como Reembolsada

**What:** Na transação atômica de cancelamento, o sistema deve executar `UPDATE Reservas SET Reembolsada = 1 WHERE Id = @reservaId`. O campo `Reembolsada` é um `BIT NOT NULL DEFAULT 0` na tabela `Reservas`.

**Why:** Registra que a reserva foi cancelada com reembolso. O cancelamento é lógico, nunca exclusão física (ADR-012).

**Acceptance Criteria:**
- [ ] Coluna `Reembolsada` existe na tabela `Reservas` (via migration `Script0013`).
- [ ] Após cancelamento bem-sucedido, `SELECT Reembolsada FROM Reservas WHERE Id = @reservaId` retorna `1`.
- [ ] O `DEFAULT 0` garante que reservas existentes têm `Reembolsada = 0` automaticamente.

### FR-006: Marcar Todos os ItensReserva como Reembolsados

**What:** Na transação atômica, o sistema deve executar `UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId`. Todos os itens (1 a 4 CPFs) são marcados de uma vez.

**Why:** Reembolso integral — a reserva inteira é cancelada, não itens individuais (reembolso granular está fora do escopo v2.0).

**Acceptance Criteria:**
- [ ] Após cancelamento, todos os registros em `ItensReserva` com `ReservaId = @reservaId` têm `Reembolsado = 1`.
- [ ] Se a reserva tem 4 itens, os 4 são atualizados.
- [ ] Se a reserva tem 1 item, apenas 1 é atualizado.

### FR-007: Liberar Ingressos Vinculados

**What:** Na transação atômica, o sistema deve executar:
```sql
UPDATE Ingressos SET Status = 0, DataBloqueio = NULL
WHERE Id IN (SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId)
```
Os ingressos voltam ao status `0` (Livre) para que outros compradores possam adquiri-los.

**Why:** As vagas voltam ao pool de disponibilidade. Ingressos com `Status=0` são considerados livres pelas queries de disponibilidade existentes.

**Acceptance Criteria:**
- [ ] Após cancelamento, todos os ingressos vinculados aos itens da reserva têm `Status = 0` e `DataBloqueio IS NULL`.
- [ ] Se `ItensReserva.IngressoId` for `NULL` (cenário hipotético — não ocorre no sistema atual), o sub-SELECT não retorna linhas e o UPDATE afeta 0 registros (comportamento seguro).
- [ ] Ingressos que não pertencem à reserva cancelada NÃO são afetados.

### FR-008: Marcar Pagamento como Reembolsado (se Existir)

**What:** Na transação atômica, o sistema deve executar:
```sql
UPDATE Pagamentos SET Status = 2 WHERE ReservaId = @reservaId AND Status = 1
```
O valor `2` corresponde a `StatusPagamento.Reembolsado` (enum `Domain.Enums.StatusPagamento`: `Pendente=0, Confirmado=1, Reembolsado=2, Falhou=3`). A condição `AND Status = 1` garante que apenas pagamentos confirmados sejam marcados como reembolsados.

**Why:** Registra o reembolso do pagamento associado. Se não houver pagamento (reserva gratuita ou não paga), o UPDATE afeta 0 linhas — comportamento seguro.

**Acceptance Criteria:**
- [ ] Reserva paga com `Pagamento.Status = 1` (Confirmado): após cancelamento, `Pagamento.Status = 2` (Reembolsado).
- [ ] Reserva gratuita sem pagamento: o UPDATE afeta 0 linhas, sem erro.
- [ ] Reserva não paga sem pagamento: o UPDATE afeta 0 linhas, sem erro.
- [ ] O valor `2` usado no SQL corresponde exatamente a `(int)StatusPagamento.Reembolsado`.

### FR-009: Transação Atômica (Tudo ou Nada)

**What:** Todas as 4 operações de UPDATE (FR-005 a FR-008) devem executar dentro de uma única transação SQL (`BEGIN TRANSACTION` / `COMMIT` / `ROLLBACK`). Se qualquer UPDATE falhar, todas as alterações são revertidas via `ROLLBACK`.

**Why:** Consistência de dados. Não pode ocorrer de ingressos serem liberados sem que a reserva seja marcada como reembolsada, ou vice-versa (ADR-012).

**Acceptance Criteria:**
- [ ] O método `CancelarComTransacao` no `ReservaRepository` usa `connection.BeginTransaction()`.
- [ ] Em caso de sucesso, chama `transaction.CommitAsync(ct)`.
- [ ] Em caso de exceção em qualquer UPDATE, chama `transaction.RollbackAsync(ct)` no bloco `catch`.
- [ ] O bloco `catch` relança a exceção após rollback (`throw;`).

### FR-010: Campo `Reembolsada` na Entidade Reserva (Domain)

**What:** A entidade `Domain.Entities.Reserva` deve ter o campo `public bool Reembolsada { get; private set; }` e o método `public void MarcarReembolsada() { Reembolsada = true; }`. O campo deve ser inicializado como `false` no construtor privado existente (linha 23 do arquivo atual).

**Why:** Encapsulamento de domínio. O estado de reembolso é uma propriedade da reserva, não um cálculo derivado.

**Acceptance Criteria:**
- [ ] `Reserva.cs` contém `public bool Reembolsada { get; private set; }`.
- [ ] Construtor privado define `Reembolsada = false` (adicionar na linha 29, junto com `Pago = false`).
- [ ] Método `MarcarReembolsada()` existe e define `Reembolsada = true`.
- [ ] Projeto `Domain` compila sem erros.

### FR-011: Migration `Script0013` (Database)

**What:** Criar `db/Script0013_AdicionarReembolsadaReservas.sql` com conteúdo idempotente:
```sql
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'Reembolsada'
)
BEGIN
    ALTER TABLE dbo.Reservas ADD Reembolsada BIT NOT NULL DEFAULT 0;
END
```
O script é executado automaticamente pelo DbUp na inicialização da API.

**Why:** Versionamento de schema. A migration adiciona a coluna sem afetar dados existentes. `DEFAULT 0` garante que reservas criadas antes da migration são consistentemente `Reembolsada = false`.

**Acceptance Criteria:**
- [ ] Arquivo `db/Script0013_AdicionarReembolsadaReservas.sql` existe.
- [ ] Script é idempotente (`IF NOT EXISTS`).
- [ ] Ao rodar a API, DbUp executa o script sem erros.
- [ ] Coluna `Reembolsada` existe na tabela `Reservas` com tipo `BIT`, `NOT NULL`, `DEFAULT 0`.

### FR-012: Atualizar `ReservaDetalhadaDTO` com Campo `Reembolsada`

**What:** O DTO `Domain.DTOs.ReservaDetalhadaDTO` deve incluir o campo `public bool Reembolsada { get; set; }`. Como `ReservaAdminDTO` herda de `ReservaDetalhadaDTO`, o campo aparece automaticamente em ambas as listagens (Admin e usuário comum).

**Why:** O frontend e os testes precisam saber se a reserva foi reembolsada para exibir o status correto e o botão de cancelamento.

**Acceptance Criteria:**
- [ ] `ReservaDetalhadaDTO` contém `public bool Reembolsada { get; set; }`.
- [ ] Projeto `Domain` compila sem erros.

### FR-013: Atualizar Queries SQL com Coluna `Reembolsada`

**What:** As queries SQL em `ReservaRepository` devem incluir `r.Reembolsada` no SELECT:
1. `ListarReservasDetalhadasPorCpf` (linha 144-158 do arquivo atual) — adicionar `r.Reembolsada` no SELECT e no GROUP BY.
2. `ListarTodasDetalhadasAdmin` (linha 169-187) — adicionar `r.Reembolsada` no SELECT e no GROUP BY.

**Why:** O Dapper faz mapeamento por nome de coluna. Se a coluna não estiver no SELECT, a propriedade `Reembolsada` do DTO fica com valor `default(bool)` = `false`, o que é incorreto para reservas já reembolsadas.

**Acceptance Criteria:**
- [ ] Query `ListarReservasDetalhadasPorCpf` seleciona `r.Reembolsada` e inclui no `GROUP BY`.
- [ ] Query `ListarTodasDetalhadasAdmin` seleciona `r.Reembolsada` e inclui no `GROUP BY`.
- [ ] Projeto `Infraestructure` compila sem erros.
- [ ] `GET /api/reserva/minhas` retorna `"reembolsada": true` para reservas canceladas e `"reembolsada": false` para reservas ativas.

### FR-014: Atualizar `ReservaVendedorDTO` com Campo `Reembolsada`

**What:** O DTO `Domain.DTOs.ReservaVendedorDTO` deve incluir o campo `public bool Reembolsada { get; set; }`.

**Why:** O Vendedor, ao visualizar suas vendas (`GET /api/reserva/minhas-vendas`), precisa saber quais reservas foram reembolsadas.

**Acceptance Criteria:**
- [ ] `ReservaVendedorDTO` contém `public bool Reembolsada { get; set; }`.
- [ ] A query `ListarReservasDetalhadasPorVendedor` inclui `r.Reembolsada` no SELECT.
- [ ] Projeto `Domain` compila sem erros.

### FR-015: Interface `IReservaRepository` — Método `CancelarComTransacao`

**What:** A interface `Domain.Interface.IReservaRepository` deve declarar o método:
```csharp
Task CancelarComTransacao(Guid reservaId, CancellationToken ct);
```
O parâmetro `Evento` não é necessário porque a transação usa apenas `reservaId` para todas as queries (as tabelas `ItensReserva`, `Ingressos`, `Pagamentos` referenciam `ReservaId`).

**Why:** Contrato do repositório para a transação de cancelamento. A interface já existe; apenas adicionar este método.

**Acceptance Criteria:**
- [ ] `IReservaRepository` declara `Task CancelarComTransacao(Guid reservaId, CancellationToken ct);`.
- [ ] Projeto `Domain` compila sem erros.

### FR-016: Interface `IReservaService` — Método `CancelarReserva`

**What:** A interface `Application.Interfaces.IReservaService` deve declarar o método:
```csharp
Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct);
```

**Why:** Contrato do serviço para a lógica de cancelamento. A interface já existe; apenas adicionar este método.

**Acceptance Criteria:**
- [ ] `IReservaService` declara `Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct);`.
- [ ] Projeto `Application` compila sem erros.

### FR-017: Implementar `CancelarReserva` no `ReservaService`

**What:** O método `CancelarReserva` no `Application.Service.ReservaService` deve executar, nesta ordem:
1. Buscar reserva via `_repositoryReserva.BuscarPorId(reservaId, ct)`. Se `null`, lançar `DomainException("Reserva não encontrada.")`.
2. Verificar propriedade: se `reserva.UsuarioCpf != usuarioCpf`, lançar `UnauthorizedAccessException("Esta reserva não pertence a você.")`.
3. Verificar reembolso prévio: se `reserva.Reembolsada`, lançar `DomainException("Reserva já foi cancelada.")`.
4. Buscar evento via `_repositoryEvento.GetByIdAsync(reserva.EventoId)`. Se `null`, lançar `DomainException("Evento não encontrado.")`.
5. Verificar data do evento: se `evento.DataEvento <= DateTime.UtcNow`, lançar `DomainException("Não é possível cancelar. O evento já começou.")`.
6. Delegar transação atômica: `await _repositoryReserva.CancelarComTransacao(reservaId, ct)`.
7. Enfileirar e-mail de reembolso (se aplicável): buscar e-mail do comprador via `_repositoryUsuario.BuscarCpf(usuarioCpf, ct)`, destino = `usuario?.Email ?? usuarioCpf`.

**Why:** Lógica de negócio central do cancelamento. Validações em ordem correta garantem fail-fast (evita buscar evento se já reembolsada).

**Acceptance Criteria:**
- [ ] Cada condição de erro (FR-002, FR-003, FR-004) lança a exceção correta com a mensagem exata.
- [ ] Caminho feliz chama `_repositoryReserva.CancelarComTransacao(reservaId, ct)` uma vez.
- [ ] O e-mail de reembolso é enfileirado APÓS a transação bem-sucedida, nunca antes.
- [ ] Se `_repositoryReserva.CancelarComTransacao` lançar exceção, o e-mail NÃO é enfileirado.
- [ ] `ReservaService` NÃO precisa de novas dependências no construtor — `IEmailSender`, `IUsuarioRepository`, `IEventoRepository`, `IReservaRepository` já estão injetados (spec 180).

### FR-018: Enfileirar E-mail de Reembolso Confirmado

**What:** Após a transação atômica de cancelamento ser concluída com sucesso, o sistema deve enfileirar um e-mail de confirmação de reembolso usando `EmailTemplates.ReembolsoConfirmado` (já existe — spec 180). O destinatário é o e-mail do comprador (buscar via `_repositoryUsuario.BuscarCpf(usuarioCpf, ct)`), com fallback para `usuarioCpf` se o usuário não for encontrado. O e-mail inclui `nomeEvento` (do objeto `evento` já carregado) e `valorReembolsado` (`reserva.ValorFinalPago`).

**Why:** "Tranquilidade financeira" (visao.md §6.9) — o comprador recebe comprovante de que o reembolso foi processado. O template já existe desde a spec 180 e está pronto para uso.

**Acceptance Criteria:**
- [ ] Após `CancelarComTransacao`, há chamada a `_emailSender.EnfileirarAsync`.
- [ ] O e-mail usa `EmailTemplates.ReembolsoConfirmado(destinatario, evento.Nome, reserva.ValorFinalPago)`.
- [ ] Se `BuscarCpf` retornar `null`, usa `usuarioCpf` como fallback para destinatário (não quebra o fluxo).
- [ ] Para reservas gratuitas (`ValorFinalPago = 0`), o e-mail é enviado com `valorReembolsado = 0` — comportamento correto (houve cancelamento, mas sem valor financeiro).
- [ ] Se `CancelarComTransacao` lançar exceção, `EnfileirarAsync` NÃO é chamado.

### FR-019: Implementar `CancelarComTransacao` no `ReservaRepository`

**What:** O método `CancelarComTransacao` no `Infraestructure.Repository.ReservaRepository` deve:
1. Criar conexão: `using var connection = _factory.CreateConnection()`.
2. Abrir conexão: `await connection.OpenAsync(ct)`.
3. Iniciar transação: `using var transaction = connection.BeginTransaction()`.
4. Executar 4 UPDATEs na ordem correta (dentro de try block):
   a. `UPDATE Reservas SET Reembolsada = 1 WHERE Id = @reservaId`
   b. `UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId`
   c. `UPDATE Ingressos SET Status = 0, DataBloqueio = NULL WHERE Id IN (SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId)`
   d. `UPDATE Pagamentos SET Status = 2 WHERE ReservaId = @reservaId AND Status = 1`
5. Commit: `await transaction.CommitAsync(ct)`.
6. No catch: `await transaction.RollbackAsync(ct); throw;`.

**Why:** Execução atômica da transação no banco. A ordem dos UPDATEs é relevante apenas para legibilidade (todos são independentes dentro da transação).

**Acceptance Criteria:**
- [ ] Método `CancelarComTransacao` existe em `ReservaRepository`.
- [ ] Usa `BeginTransaction()` / `CommitAsync` / `RollbackAsync`.
- [ ] Todos os 4 UPDATEs usam `new CommandDefinition(sql, new { reservaId }, transaction, cancellationToken: ct)`.
- [ ] O valor de `Status` no UPDATE de Pagamentos é `2` (não `3`), correspondendo a `(int)StatusPagamento.Reembolsado`.
- [ ] Projeto `Infraestructure` compila sem erros.

---

## Non-Functional Requirements

### NFR-001: Atomicidade da Transação

**What:** As 4 operações de UPDATE (FR-005 a FR-008) devem ser atômicas — ou todas executam com sucesso, ou nenhuma é aplicada. A transação SQL (`BEGIN/COMMIT/ROLLBACK`) garante isso.

**Acceptance Criteria:**
- [ ] Se qualquer UPDATE falhar (ex: constraint violation, deadlock), todas as alterações são revertidas.
- [ ] Teste: simular falha no 3º UPDATE → verificar que os 2 primeiros foram revertidos.

### NFR-002: Idempotência do Endpoint

**What:** Chamadas repetidas de `DELETE /api/reserva/{id}` para a mesma reserva não podem causar efeitos colaterais duplicados. A validação `Reembolsada == true` (FR-004) garante idempotência — a segunda chamada retorna `409 Conflict`.

**Acceptance Criteria:**
- [ ] Primeira chamada: `200 OK`, dados alterados.
- [ ] Segunda chamada (idêntica): `409 Conflict`, dados inalterados.
- [ ] Chamadas concorrentes (race condition): apenas uma transação efetiva as mudanças; a outra retorna `409 Conflict`.

### NFR-003: Segurança — CPF do JWT, Nunca do Body

**What:** O identificador do usuário (`usuarioCpf`) é sempre extraído da claim `"cpf"` do JWT (`User.Claims`), nunca de parâmetro de rota, query string ou body da requisição. Consistente com ADR-004 e ADR-012.

**Acceptance Criteria:**
- [ ] Controller não recebe `cpf` como parâmetro do método.
- [ ] Service recebe `usuarioCpf` como parâmetro, passado pelo controller após extração do JWT.
- [ ] Nenhum DTO de request inclui campo `cpf` para o endpoint de cancelamento.

### NFR-004: Performance — Resposta da API < 500ms (p95)

**What:** O endpoint de cancelamento deve responder em menos de 500ms no percentil 95. O envio de e-mail é fire-and-forget (não bloqueia a resposta). A transação SQL opera com 4 UPDATEs simples, indexadas por PK/FK.

**Acceptance Criteria:**
- [ ] `EnfileirarAsync` é a última operação antes do return — o envio real (SMTP) ocorre em background.
- [ ] A transação SQL não envolve locks prolongados — todas as tabelas têm índices nas colunas usadas nas cláusulas WHERE.

---

## Constraints

- **C1:** O campo `Reembolsada` na tabela `Reservas` deve ser `BIT NOT NULL DEFAULT 0`. Migration `Script0013` via DbUp, padrão idempotente (`IF NOT EXISTS`).
- **C2:** `StatusPagamento.Reembolsado = 2` no enum atual (`Domain/Enums/StatusPagamento.cs`). O SQL deve usar valor `2` (não `3`).
- **C3:** Todas as queries SQL usam Dapper com parâmetros (`@reservaId`) — proteção contra SQL Injection (ADR-002).
- **C4:** `Ingresso.Status = 0` significa Livre. `Ingresso.Status = 1` significa Reservado. `Ingresso.Status = 2` significa Vendido. O cancelamento define `Status = 0` e `DataBloqueio = NULL`.
- **C5:** O reembolso é simulado (status no banco) — sem gateway de pagamento real (ADR-012). O escopo atual não inclui estorno financeiro.
- **C6:** O `ReservaService` já injeta `IEmailSender`, `IUsuarioRepository`, `IEventoRepository`, `IReservaRepository` (spec 180). NÃO adicionar novos parâmetros ao construtor.
- **C7:** Nomes de arquivos, namespaces e classes seguem o padrão do projeto: PascalCase, namespaces alinhados com a estrutura de pastas.
- **C8:** O projeto deve compilar em .NET 9 sem erros. `dotnet build` deve sair com código 0.

---

## Edge Cases & Error States

| # | Caso | Comportamento Esperado |
|---|------|------------------------|
| **E1** | Reserva inexistente (`Guid` aleatório) | `BuscarPorId` retorna `null` → `DomainException("Reserva não encontrada.")` → controller retorna `404 Not Found`. |
| **E2** | Reserva de outro usuário (`UsuarioCpf != cpfJWT`) | `UnauthorizedAccessException("Esta reserva não pertence a você.")` → controller retorna `403 Forbidden`. |
| **E3** | Evento já começou (`DataEvento <= DateTime.UtcNow`) | `DomainException("Não é possível cancelar. O evento já começou.")` → controller retorna `400 Bad Request`. |
| **E4** | Reserva já reembolsada (`Reembolsada == true`) | `DomainException("Reserva já foi cancelada.")` → controller retorna `409 Conflict`. |
| **E5** | Evento cancelado pelo vendedor antes do cancelamento da reserva | Comportamento depende da spec 50: se spec 50 marca `Reembolsada = true` ao cancelar evento, spec 40 retorna `409`. Se spec 50 não marcar, spec 40 procede com cancelamento normalmente (transação inofensiva — UPDATEs sem efeito real). **A implementação da spec 40 não inclui lógica específica para este caso.** |
| **E6** | Reserva com múltiplos itens (até 4 CPFs) | Todos os `ItensReserva` são marcados `Reembolsado = 1`. Todos os ingressos vinculados são liberados (`Status = 0`). |
| **E7** | Reserva gratuita (`ValorFinalPago = 0`) sem pagamento | UPDATE de `Pagamentos` afeta 0 linhas — seguro. E-mail de reembolso é enviado com `valorReembolsado = 0`. Reserva é marcada como reembolsada. |
| **E8** | Reserva não paga (`Pago = false`) sem registro em `Pagamentos` | UPDATE de `Pagamentos` afeta 0 linhas — seguro. Demais operações executam normalmente. |
| **E9** | Cancelamento concorrente (duas requests simultâneas para a mesma reserva) | Ambas leem `Reembolsada = false`. A primeira transação faz COMMIT e altera `Reembolsada = 1`. A segunda transação tenta fazer COMMIT — como `Reembolsada` já é `1`, os UPDATEs são inofensivos. **Comportamento aceitável**: ambas retornam `200 OK` (a segunda é no-op). Alternativamente, se a spec 50 adicionar lock, apenas a primeira transação procede. Para a spec 40, o comportamento atual é aceitável porque as operações são idempotentes. |
| **E10** | Usuário não autenticado (sem JWT) | `[Authorize]` no controller rejeita com `401 Unauthorized` antes de chegar ao método. |
| **E11** | JWT sem claim `"cpf"` | Controller retorna `401 Unauthorized` com `{ "message": "CPF não encontrado no token." }`. |
| **E12** | Evento não encontrado (inconsistência: reserva existe mas evento foi excluído) | `_repositoryEvento.GetByIdAsync` retorna `null` → `DomainException("Evento não encontrado.")` → controller retorna `400 Bad Request` (via `GlobalExceptionHandlerMiddleware`). |
| **E13** | `BuscarCpf` retorna null (usuário deletou a conta) | Fallback: usa `usuarioCpf` como destinatário do e-mail. E-mail não será entregue (não é um endereço válido), mas o fluxo não quebra. Logar warning seria ideal, mas não obrigatório para esta spec. |
| **E14** | SMTP não configurado (graceful degradation da spec 180) | `EmailBackgroundWorker` descarta a mensagem com `LogWarning`. Cancelamento é efetivado normalmente. |

---

## Dependencies

### Internal (Specs do Projeto)
- **Spec 120 (Segurança):** BCrypt e JWT implementados. `[Authorize]` e extração de claims do JWT funcionais.
- **Spec 130 (Isolamento Multi-Tenant):** `ReservaRepository` já filtra por `VendedorCpf`. O cancelamento não interfere nesse isolamento.
- **Spec 150 (Resiliência):** `GlobalExceptionHandlerMiddleware` captura exceções não tratadas e retorna respostas padronizadas.
- **Spec 170 (Pagamento Simulado):** `StatusPagamento.Reembolsado = 2` definido. `Pagamento` entity com `MarcarReembolsado()` existente.
- **Spec 180 (E-mail Transacional):** `IEmailSender` já injetado em `ReservaService`. `EmailTemplates.ReembolsoConfirmado` já existe. `EmailBackgroundWorker` em produção.
- **Spec 50 (Cancelamento de Evento):** Dependência lógica (spec 40 é pré-requisito para spec 50), mas não dependência de build. Spec 50 usará `CancelarComTransacao` ou método similar para reembolsar múltiplas reservas de uma vez.

### External (Pacotes NuGet)
- Nenhum novo pacote necessário. Dapper (já existente) para queries. MailKit (já existente, spec 180) para e-mail.

### Configuration
- Nenhuma nova configuração necessária. `appsettings.json` não precisa de alterações.

---

## Out of Scope

- Gateway de pagamento real (reembolso é simulado via status no banco).
- Reembolso parcial (ItemReserva individual) — a reserva inteira é cancelada.
- Cancelamento de evento pelo Vendedor — pertence à spec 50.
- Visão unificada de cancelamento (flags `podeCancelar`, `reembolsado` por item) — pertence à spec 110.
- Política de prazo mínimo para cancelamento (ex: até 24h antes) — a única restrição é `DataEvento > agora`.
- Estorno financeiro real (integração com Stripe/PagSeguro).
- Notificação ao Vendedor sobre cancelamento de reserva do seu evento.
- Exclusão física de dados (cancelamento é lógico — ADR-012).
- Endpoint para Admin cancelar reservas de terceiros — pertence à spec 110.
