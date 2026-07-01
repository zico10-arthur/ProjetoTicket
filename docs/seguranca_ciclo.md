# Segurança no Ciclo de Desenvolvimento

---

## 🔐 Threat Model

### Rota de Maior Risco: `POST /api/eventos/{id}/cancelar` — Cancelamento de Evento com Reembolso

| Campo | Descrição |
|:---|:---|
| **Ativos Protegidos** | (1) **Dados financeiros dos compradores** — valores de pagamento, histórico de reembolsos e saldos. (2) **Integridade da transação atômica** — consistência entre as 5 tabelas envolvidas (Eventos, Ingressos, Reservas, ItensReserva, Pagamentos). (3) **Identidade e autorização do vendedor** — apenas o vendedor dono do evento (ou Admin) pode cancelá-lo. (4) **Disponibilidade da rota** — impacto em todos os compradores do evento caso a operação falhe. |
| **Vetor de Ataque Provável** | Um atacante autenticado como vendedor mal-intencionado tenta cancelar evento de outro vendedor manipulando o `{id}` da rota (IDOR — Insecure Direct Object Reference), burlando a verificação de propriedade `VendedorId`. Alternativamente, exploração de race condition: disparar múltiplas requisições simultâneas de cancelamento para o mesmo evento, tentando forçar inconsistência entre as tabelas ou disparar reembolsos duplicados antes do commit da transação. |
| **Falha Arquitetural Potencial** | Ausência de verificação de propriedade (`VendedorId` do token JWT vs. `VendedorId` do evento) antes da execução da transação atômica, permitindo que qualquer vendedor autenticado cancele eventos alheios. Agravado pela ausência de lock pessimista ou optimistic concurrency control na tabela `Eventos`, expondo a transação a race conditions e possível duplicação de reembolsos. |
| **Controle de Engenharia (Mitigação)** | (1) **Verificação de propriedade no início do handler**: extrair `VendedorId` do token JWT e comparar com `VendedorId` do evento antes de qualquer operação — se divergir, retornar `403 Forbidden`. (2) **Lock otimista via rowversion**: adicionar coluna `RowVersion` na tabela `Eventos` e validar no `UPDATE` dentro da transação, abortando se o registro foi alterado concorrentemente. (3) **Idempotência**: verificar se o evento já está com status cancelado antes de iniciar a transação, retornando `409 Conflict` para requisições repetidas. (4) **Log de auditoria**: registrar toda tentativa de cancelamento (bem-sucedida ou não) com `VendedorId`, `EventoId`, timestamp e IP de origem. |

---

## 🚧 Gates de Segurança

A equipe adotará os seguintes gates de segurança obrigatórios ao longo do ciclo de desenvolvimento:

### Gate 1 — Verificação de Identidade e Autorização em Todo Endpoint

**Critério de passagem:** Toda nova rota ou rota alterada deve implementar validação de identidade (JWT) e autorização (claim-based) **antes** de qualquer lógica de negócio. Nenhum endpoint que manipule dados de terceiros pode operar sem verificar se o chamador é o proprietário do recurso (`VendedorId`, `CompradorId`, `AdminId`).

**Checklist:**
- [x] Token JWT validado (assinatura, expiração, issuer)
- [x] Claims extraídas e mapeadas para identidade do usuário
- [x] Verificação de propriedade do recurso (`VendedorId` do token == `VendedorId` do registro)
- [x] Teste automatizado comprovando que usuário não-proprietário recebe `403 Forbidden`

---

### Gate 2 — Proteção contra Injeção e Exposição de Dados

**Critério de passagem:** Toda query SQL deve usar parâmetros (Dapper/SQL Server) — zero concatenação de strings. Nenhum dado sensível (senha, token, chave) pode ser retornado em respostas de API, logs ou mensagens de erro.

**Checklist:**
- [x] Zero concatenação de strings em queries SQL (100% parâmetros Dapper)
- [x] Senhas armazenadas apenas como hash BCrypt (nunca plain text ou reversível)
- [x] Nenhum campo `Senha`, `Token` ou `Chave` nos DTOs de resposta
- [x] Mensagens de erro genéricas para o cliente (detalhes técnicos apenas em log interno)
- [x] Scan automatizado de segredos no pipeline (detecta `Password=`, `Pwd=`, secrets em arquivos)

---

### Gate 3 — Revisão de Segurança e Testes de Resiliência Antes do Merge

**Critério de passagem:** Todo Pull Request que envolva lógica de pagamento, reembolso, cancelamento ou alteração de permissões deve passar por revisão de segurança específica e incluir testes que cubram cenários de ataque conhecidos.

**Checklist:**
- [x] Code review por pelo menos 2 membros do time, sendo um focado em segurança
- [x] Testes unitários/integração para cenários: IDOR, race condition (concorrência), idempotência e entradas maliciosas
- [x] Teste de regressão no endpoint `POST /api/eventos/{id}/cancelar` (rota crítica)
- [x] Verificação de que transações atômicas possuem `try/catch` com `ROLLBACK` em caso de falha
- [x] Nenhum alerta de segurança pendente em ferramentas de análise estática (SAST)
