# Prompt — Frontend SoldOut Tickets (Blazor Server + MudBlazor)

> **Uso:** Copie este documento inteiro como prompt para uma IA (ou dev) implementar/atualizar o frontend.
> **Branch de trabalho:** `frontend` (integrada com `main` em 27/06/2026)
> **Documentação de referência:** [`visao.md`](./visao.md) · [`storytelling.md`](./storytelling.md) · [`arquitetura.md`](./arquitetura.md) · [`sprints.md`](./sprints.md) · [`agents/roadmap.md`](./agents/roadmap.md)

---

## 1. Contexto do Projeto

Você está desenvolvendo o **frontend** do **SoldOut Tickets** — plataforma SaaS de venda de ingressos para eventos de pequeno porte (palestras, workshops, teatros).

| Camada | Stack | Porta local |
|--------|-------|-------------|
| **API** | ASP.NET Core 9, Clean Architecture, JWT, SQL Server | `http://localhost:5007` |
| **Frontend** | Blazor Server, MudBlazor, HttpClient | `http://localhost:5057` |
| **Swagger** | Documentação interativa da API | `http://localhost:5007/swagger` |

### Estado atual do backend (v2.0) — ~90% concluído

| Área | Status | Specs |
|------|--------|-------|
| Auto cadastro vendedor, login unificado, 3 perfis | ✅ | ST-01, ST-08, ST-09, ST-10 |
| Tipos Teatro/Palestra, evento gratuito | ✅ | ST-03, ST-11 |
| Reserva multi-participante (ItemReserva, até 4 CPFs) | ✅ | ST-04 |
| Pagamento simulado (checkout interno) | ✅ | 170 |
| Admin/Vendedor podem comprar | ✅ | ST-07 |
| Segurança (BCrypt, JWT user-secrets, rate limit) | ✅ | 120 |
| Isolamento multi-tenant (VendedorCpf) | ✅ | 130 |
| Cupons — AdminId via JWT | ✅ | 160 |
| Resiliência de erros | ✅ | 150 |
| E-mail transacional + esqueci senha | ✅ | 180 |
| Cancelamento de reserva c/ reembolso | ✅ | ST-05 |
| Docker + Hangfire | ✅ | 140, 190 |
| Cancelamento de evento c/ reembolso | ❌ pendente | ST-06 |
| Cancelamento unificado (endpoints extras) | ❌ pendente | ST-12 |

> **ST-02 (Painel do Vendedor)** é **100% frontend** — não existe spec de backend. É a principal entrega pendente de UI.

---

## 2. O que já existe no frontend (`Web/`)

### 2.1 Infraestrutura

- **Auth:** JWT em `localStorage` (`authToken`), `CustomAuthStateProvider`, roles `Admin`, `Vendedor`, `Comprador`
- **HttpClient:** BaseAddress `http://localhost:5007/` em `Program.cs`
- **Layout:** `MainLayout.razor` com navbar por perfil + tema customizado `TicketPrimeTheme`
- **Docker:** `Web/Dockerfile` pronto

### 2.2 Páginas existentes (parcialmente atualizadas)

| Rota | Arquivo | Status |
|------|---------|--------|
| `/` | `Home.razor` | ⚠️ Lista eventos da API + fallback mock + link demo assentos |
| `/login` | `Login.razor` | ⚠️ Funciona, falta redirect por perfil e link "Esqueci senha" |
| `/cadastro` | `Cadastro.razor` | ⚠️ Só Comprador — falta cadastro Vendedor |
| `/comprar/{id}/ingressos` | `ComprarIngressos.razor` | ⚠️ Mapa de bolinhas (Teatro) — não suporta Palestra/ItemReserva |
| `/checkout/{eventoId}/{ingressoId}` | `CheckoutReserva.razor` | ⚠️ Cupom OK, mas não cria reserva no formato v2 |
| `/pagamento/{eventoId}/{ingressoId}` | `Pagamento.razor` | ❌ Endpoints **obsoletos** — precisa reescrever |
| `/reservas` | `MinhasReservas.razor` | ❌ Endpoint **obsoleto** — falta cancelamento |
| `/perfil` | `Perfil.razor` | ⚠️ CRUD básico funciona |
| `/cupons` | `Cupons.razor` | ✅ Lista cupons válidos |
| `/eventos/criar` | `Vendedor/CriarEvento.razor` | ❌ Falta Tipo, Gratuito, Local, Descrição |
| `/eventos/meus` | `Vendedor/MeusEventos.razor` | ⚠️ Lista e delete — falta cancelamento c/ alerta |
| `/eventos/{id}/ingressos` | `Vendedor/IngressosEvento.razor` | ⚠️ Visualização de ingressos |
| `/admin/*` | Várias | ⚠️ Cupons OK (JWT), CadastrarVendedor **obsoleto** |
| `/demo/assentos` | `DemoAssentos.razor` | ✅ Demo visual do SeatMap (mock) |
| `/selecionar-assentos/{id}` | `SelecionarAssentos.razor` | ❌ Esqueleto — não conectado à API |

### 2.3 Componentes novos (branch frontend)

```
Web/Components/Features/SeatMap/
├── SeatMap.razor          → Mapa visual de auditório (palco, filas, corredor)
├── SeatItem.razor         → Assento individual clicável
├── SeatMapMapper.cs       → Converte ingressos API → SeatModel
├── SeatModel.cs / SeatStatus.cs / SeatBlock.cs
└── QuintaAuditorioSeatData.cs → Layout mock UNIFESO

Web/Theme/TicketPrimeTheme.cs  → Paleta MudBlazor customizada
Web/Mock/TicketPrimeMockData.cs → Eventos mock para demo offline
```

> O **SeatMap** está pronto visualmente, mas **não está integrado** ao fluxo real de compra. Substituir ou complementar o mapa de "bolinhas" em `ComprarIngressos.razor`.

---

## 3. Regras de negócio que o frontend DEVE respeitar

Leia [`storytelling.md`](./storytelling.md) e [`visao.md`](./visao.md). Resumo obrigatório:

### 3.1 Perfis e autenticação

- **3 perfis:** Admin (`A1A1...`), Vendedor (`B2B2...`), Comprador (`C3C3...`)
- **Login único:** `POST /api/usuario/login` → `{ token, usuario: { cpf, nome, email, perfil } }`
- **Após login:** redirecionar por perfil:
  - Comprador → `/`
  - Vendedor → `/eventos/meus` (Painel do Vendedor — ST-02)
  - Admin → `/admin/eventos`
- **Header Authorization:** `Bearer {token}` em todas as chamadas autenticadas
- Token expira — tratar 401 redirecionando para `/login?returnUrl=...`

### 3.2 Tipos de evento

```csharp
public enum TipoEvento { Teatro = 0, Palestra = 1 }
```

| Tipo | Comportamento na compra |
|------|------------------------|
| **Teatro** | Assentos numerados VIP/Geral. Comprador escolhe assento(s) no mapa. Cada item da reserva tem `IngressoId`. |
| **Palestra** | Assentos numerados setor "Geral". Mesmo fluxo de seleção, até 4 participantes por reserva. |
| **Gratuito** (`PrecoPadrao == 0` ou `Gratuito: true`) | Pular pagamento. Após `POST /api/reserva/criar`, confirmar direto e redirecionar para `/reservas`. |
| **Pago** | Após criar reserva → `POST /api/pagamento/checkout/{reservaId}` |

### 3.3 Reserva multi-participante (ST-04)

```json
POST /api/reserva/criar
{
  "eventoId": "guid",
  "itens": [
    { "cpfParticipante": "12345678901", "ingressoId": "guid" },
    { "cpfParticipante": "98765432100", "ingressoId": "guid" }
  ],
  "cupomCodigo": "PROMO10"  // opcional, rejeitado em evento gratuito
}
```

- Mínimo 1, máximo **4 itens** por reserva
- CPFs dos participantes **não precisam** ter conta no sistema
- Validar CPF no frontend (11 dígitos, máscara `000.000.000-00`)

### 3.4 Cancelamento (ST-05 — backend pronto)

```
DELETE /api/reserva/{id}
Authorization: Bearer {token}
```

- Só o **dono** da reserva pode cancelar (CPF do JWT = UsuarioCpf)
- Bloqueado se `DataEvento <= agora`
- Resposta inclui reembolso para eventos pagos
- UI: botão "Cancelar Reserva" em `/reservas` com `MudDialog` de confirmação
- Exibir badges: `Pago`, `Reembolsada` (campos no DTO)

### 3.5 Cadastro

| Fluxo | Endpoint | Campos |
|-------|----------|--------|
| Comprador | `POST /api/usuario/CadastrarComprador` | Nome, Email, Cpf, Senha |
| Vendedor (público) | `POST /api/usuario/cadastrar-vendedor` | Cnpj, RazaoSocial, NomeFantasia, Email, Senha, Telefone |
| Admin | ❌ Não existe UI — seed SQL only |

### 3.6 Esqueci minha senha (spec 180)

```
POST /api/usuario/esqueci-senha   { "email": "..." }
POST /api/usuario/redefinir-senha { "token": "...", "novaSenha": "..." }
```

Criar páginas `/esqueci-senha` e `/redefinir-senha?token=...`

### 3.7 Cupons (Admin)

- AdminId **não vai mais na rota/body** — extraído do JWT no servidor
- `POST /api/cupom/CadastrarCupom` — body: `{ codigo, porcentagemDesconto, valorMinimo, dataExpiracao }`
- Páginas Admin em `Web/Components/Pages/Admin/Cupons.razor` já estão corretas

---

## 4. Mapa completo de endpoints da API

Use o Swagger como fonte de verdade. Referência:

### Usuario
| Método | Rota | Auth | Uso no frontend |
|--------|------|------|-----------------|
| POST | `/api/usuario/CadastrarComprador` | — | `/cadastro` |
| POST | `/api/usuario/cadastrar-vendedor` | — | `/cadastro-vendedor` (criar) |
| POST | `/api/usuario/login` | — | `/login` |
| POST | `/api/usuario/esqueci-senha` | — | `/esqueci-senha` |
| POST | `/api/usuario/redefinir-senha` | — | `/redefinir-senha` |
| GET | `/api/usuario/Todos` | Admin | `/admin/usuarios` |
| GET | `/api/usuario/ListarUsuarioEspecifico/{cpf}` | Auth | `/perfil` |
| PUT | `/api/usuario/alterarsenha/{cpf}` | Auth | `/perfil` |
| PUT | `/api/usuario/alterarnome/{cpf}` | Auth | `/perfil` |
| PUT | `/api/usuario/alteraremail/{cpf}` | Auth | `/perfil` |
| DELETE | `/api/usuario/DeletarUsuario/{cpf}` | Auth | `/perfil` |

### Evento
| Método | Rota | Auth | Uso |
|--------|------|------|-----|
| GET | `/api/evento` | — | Home (eventos ativos) |
| GET | `/api/evento/{id}` | — | Detalhe do evento |
| GET | `/api/evento/meus` | Vendedor | Meus Eventos |
| POST | `/api/evento` | Vendedor | Criar Evento |
| PUT | `/api/evento/{id}` | Vendedor | Editar Evento |
| DELETE | `/api/evento/{id}` | Vendedor/Admin | Cancelar/Excluir |

### Ingresso
| Método | Rota | Auth | Uso |
|--------|------|------|-----|
| GET | `/api/ingresso/eventos/{eventoId}/ingressos` | — | Mapa de assentos |
| GET | `/api/ingresso/{id}` | — | Detalhe assento |

### Reserva
| Método | Rota | Auth | Uso |
|--------|------|------|-----|
| POST | `/api/reserva/criar` | Auth | Checkout |
| GET | `/api/reserva/minhas` | Auth | Minhas Reservas |
| GET | `/api/reserva/minhas-vendas` | Vendedor | Painel vendas |
| GET | `/api/reserva/Admin/Todas` | Admin | Admin Reservas |
| DELETE | `/api/reserva/{id}` | Auth | Cancelar reserva |

### Pagamento
| Método | Rota | Auth | Uso |
|--------|------|------|-----|
| POST | `/api/pagamento/checkout/{reservaId}` | Auth | `{ "metodo": "pix" }` |
| GET | `/api/pagamento/admin/todos` | Admin | Relatório |

### Cupom
| Método | Rota | Auth | Uso |
|--------|------|------|-----|
| GET | `/api/cupom/ListarCuponsValidos` | — | Checkout + `/cupons` |
| GET | `/api/cupom/ListarTodosCupons` | Admin | Admin cupons |
| POST | `/api/cupom/CadastrarCupom` | Admin | Criar cupom |
| DELETE | `/api/cupom/DeletarCupom/{codigo}` | Admin | — |
| PATCH | `/api/cupom/{codigo}/...` | Admin | Editar |

---

## 5. DTOs que o frontend deve usar

Reutilize os DTOs de `Application/DTOs/` via referência de projeto (já configurado no `Web.csproj`).

### EventoResponseDTO (atualizar EventoViewModel)
```csharp
public class EventoViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; }
    public DateTime DataEvento { get; set; }
    public int CapacidadeTotal { get; set; }
    public decimal PrecoPadrao { get; set; }
    public int Tipo { get; set; }           // 0=Teatro, 1=Palestra
    public bool Gratuito { get; set; }
    public bool Cancelado { get; set; }
    public string? Descricao { get; set; }
    public string? Local { get; set; }
}
```

### ReservaDetalhadaDTO (usar de Domain/DTOs/)
```csharp
public class ReservaDetalhadaDTO
{
    public Guid Id { get; set; }
    public string NomeEvento { get; set; }
    public DateTime DataEvento { get; set; }
    public string PosicaoIngresso { get; set; }
    public string SetorIngresso { get; set; }
    public string? CupomUtilizado { get; set; }
    public decimal ValorFinalPago { get; set; }
    public bool Pago { get; set; }
    public bool Reembolsada { get; set; }
}
```

---

## 6. Fluxos de UI a implementar (prioridade)

### 🔴 P0 — Corrigir integrações quebradas

1. **MinhasReservas.razor**
   - Trocar `api/Reserva/ListarPorCpf` → `api/reserva/minhas`
   - Adicionar botão cancelar → `DELETE api/reserva/{id}`
   - Mostrar status `Pago`, `Reembolsada`
   - Suportar reservas com múltiplos itens (lista de participantes)

2. **Pagamento.razor** — reescrever fluxo:
   ```
   1. POST api/reserva/criar  →  reservaId
   2. Se evento.Gratuito → redirect /reservas
   3. Senão POST api/pagamento/checkout/{reservaId}  →  confirmação
   4. Redirect /reservas
   ```
   - Remover endpoints mortos: `FazerReserva`, `ConfirmarPagamento`

3. **CheckoutReserva.razor**
   - Adicionar formulário de 1–4 CPFs participantes
   - Passar `reservaId` para pagamento (não mais eventoId/ingressoId na URL de pagamento)
   - Nova rota sugerida: `/checkout/{eventoId}` com estado dos assentos selecionados

4. **Admin/CadastrarVendedor.razor**
   - Remover `adminId` da URL
   - Usar `POST api/usuario/cadastrar-vendedor` (público) OU remover página (vendedor se auto-cadastra)

### 🟠 P1 — Funcionalidades v2.0 ausentes

5. **Cadastro Vendedor** — nova página `/cadastro-vendedor`
   - Link "Quero Vender" na Home e navbar (usuário não logado)
   - Campos: CNPJ, Razão Social, Nome Fantasia, Email, Senha, Telefone
   - Após sucesso → `/login`

6. **CriarEvento.razor / EditarEvento.razor**
   - Select Tipo: Teatro / Palestra
   - Toggle ou campo PrecoPadrao = 0 para Gratuito
   - Campos Local, Descrição
   - Min preço 0 (não 0.01) para eventos gratuitos

7. **ComprarIngressos.razor** — integrar SeatMap
   - Carregar ingressos de `GET api/ingresso/eventos/{id}/ingressos`
   - Mapear com `SeatMapMapper` para `SeatMap.razor`
   - Permitir seleção de **até 4 assentos**
   - Cada assento selecionado pede CPF do participante
   - Botão "Continuar" → `/checkout/{eventoId}`

8. **Login.razor**
   - Redirect por perfil após login
   - Link "Esqueci minha senha" → `/esqueci-senha`

9. **EsqueciSenha.razor + RedefinirSenha.razor** (novas)

### 🟡 P2 — ST-02 Painel do Vendedor (frontend puro)

10. **Dashboard Vendedor** em `/eventos/meus` ou `/vendedor/painel`:
    - Cards: total eventos, ingressos vendidos, receita estimada
    - Seção "Meus Eventos" (já existe parcialmente)
    - Seção "Vendas Recentes" via `GET api/reserva/minhas-vendas`
    - Seção "Configurações de Perfil": logo, descrição, site (campos em Usuario — editar via perfil)
    - Botão "Cancelar Evento" com dialog de confirmação (backend ST-06 pendente — preparar UI)

11. **Home.razor**
    - Remover ou isolar modo demo atrás de flag `#if DEBUG`
    - Filtrar eventos `Cancelado == false`
    - Badge "Gratuito" / tipo Teatro|Palestra no `CartaoEvento`
    - CTA "Quero Vender" para não logados

### 🟢 P3 — Polish

12. Tratamento de erros padronizado (`{ message: "..." }` do middleware)
13. Loading states consistentes (MudProgressLinear/Circular)
14. Responsividade mobile do SeatMap
15. Remover páginas template: `Counter.razor`, `Weather.razor`

---

## 7. Padrões de código exigidos

### 7.1 Estrutura de pastas (seguir existente)

```
Web/
├── Auth/                    → CustomAuthStateProvider
├── Components/
│   ├── Features/SeatMap/    → Componentes de domínio reutilizáveis
│   ├── Layout/              → MainLayout, NavMenu
│   ├── Pages/               → Rotas (@page)
│   │   ├── Admin/
│   │   └── Vendedor/
│   └── Shared/              → CartaoEvento, etc.
├── Models/                  → ViewModels (ou usar Application.DTOs diretamente)
├── Theme/                   → MudBlazor theme
└── Program.cs
```

### 7.2 Autenticação em páginas

```csharp
// Padrão para obter token e setar header
var token = await JS.InvokeAsync<string>("localStorage.getItem", "authToken");
if (string.IsNullOrEmpty(token)) { NavManager.NavigateTo("/login"); return; }
Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

Ou use `[Authorize(Roles = "Vendedor")]` com `<AuthorizeView>` como em `CriarEvento.razor`.

### 7.3 Tratamento de erro da API

```csharp
if (!response.IsSuccessStatusCode)
{
    var json = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(json);
    var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "Erro desconhecido.";
    Snackbar.Add(msg, Severity.Error);
}
```

### 7.4 UI — MudBlazor

- Usar `TicketPrimeTheme.Theme` no `MainLayout` (já configurado)
- Componentes: `MudCard`, `MudButton`, `MudTextField`, `MudSelect`, `MudDialog`, `MudSnackbar`
- Nome da marca na navbar: **SoldOut Tickets** (não TicketPrime)
- Ícones: `Icons.Material.Filled.*`

---

## 8. Fluxo de compra completo (target)

```mermaid
flowchart TD
    A[Home - lista eventos] --> B{Logado?}
    B -->|Não| C[/login?returnUrl=...]
    B -->|Sim| D[/comprar/eventoId/ingressos]
    D --> E[SeatMap - selecionar até 4 assentos]
    E --> F[Informar CPF de cada participante]
    F --> G[/checkout/eventoId]
    G --> H{Aplicar cupom?}
    H --> I[POST /api/reserva/criar]
    I --> J{Evento gratuito?}
    J -->|Sim| K[/reservas - confirmado]
    J -->|Não| L[/pagamento/reservaId]
    L --> M[POST /api/pagamento/checkout/reservaId]
    M --> K
```

---

## 9. Como rodar e testar

```bash
# Terminal 1 — API
cd Api && dotnet run
# → http://localhost:5007/swagger

# Terminal 2 — Frontend
cd Web && dotnet run
# → http://localhost:5057

# Ou Docker
docker-compose up -d
# API:5007, Web:5057
```

### Credenciais de teste (seed do banco)

Consulte `Infraestructure/DataBase/DatabaseSeeder.cs` — Admin, Vendedor e Comprador são criados no primeiro run.

### Checklist de teste manual

- [ ] Comprador: cadastro → login → comprar Teatro → pagar → ver reserva → cancelar
- [ ] Comprador: comprar Palestra com 3 CPFs diferentes
- [ ] Comprador: evento gratuito — pula pagamento
- [ ] Comprador: aplicar cupom no checkout
- [ ] Vendedor: auto cadastro → criar evento Teatro e Palestra → ver vendas
- [ ] Admin: CRUD cupons, ver todas reservas
- [ ] Esqueci senha → e-mail (SMTP configurado em appsettings)
- [ ] SeatMap reflete status real dos ingressos (livre/reservado/vendido)

---

## 10. O que NÃO fazer

- ❌ Não criar endpoints novos no backend — o frontend consome a API existente
- ❌ Não passar `AdminId` em rotas de cupom (spec 160)
- ❌ Não usar endpoints removidos: `ListarPorCpf`, `FazerReserva`, `ConfirmarPagamento`, `CadastrarVendedor/{adminId}`
- ❌ Não permitir cupom em evento gratuito
- ❌ Não permitir mais de 4 participantes por reserva
- ❌ Não hardcodar JWT ou secrets
- ❌ Não quebrar o Docker build (`Web/Dockerfile`)

---

## 11. Entregáveis esperados

1. Todas as páginas P0 e P1 funcionando contra a API v2.0
2. SeatMap integrado ao fluxo real de compra
3. Painel do Vendedor (ST-02) com vendas e gestão de eventos
4. Fluxo de auth completo (login, cadastros, esqueci senha)
5. UI consistente com MudBlazor + TicketPrimeTheme + marca SoldOut Tickets
6. Zero chamadas a endpoints obsoletos

---

> **Última atualização:** 27/06/2026 · Branch `frontend` = `main` + componentes SeatMap/Tema/Mock
