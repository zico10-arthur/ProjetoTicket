# Prompt — Frontend Plateia (Blazor Server + MudBlazor)

> **Uso:** Copie este documento inteiro como prompt para uma IA (ou dev) implementar/atualizar o frontend.
> **Branch de trabalho:** `frontend` (integrada com `main` em 27/06/2026)
> **Documentação de referência:** [`visao.md`](./visao.md) · [`storytelling.md`](./storytelling.md) · [`arquitetura.md`](./arquitetura.md) · [`sprints.md`](./sprints.md) · [`agents/roadmap.md`](./agents/roadmap.md)

---

## 1. Contexto do Projeto

Você está desenvolvendo o **frontend** do **Plateia** — plataforma SaaS de venda de ingressos para eventos de pequeno porte (palestras, workshops, teatros).

| Camada | Stack | Porta local |
|--------|-------|-------------|
| **API** | ASP.NET Core 9, Clean Architecture, JWT, SQL Server | `http://localhost:5007` |
| **Frontend** | Blazor Server, MudBlazor, HttpClient | `http://localhost:5057` |
| **Swagger** | Documentação interativa da API | `http://localhost:5007/swagger` |

### Identidade visual (implementada)

| Elemento | Valor |
|----------|-------|
| **Marca** | Plateia |
| **Paleta** | Roxo `#6B21A8` · Ciano `#0D9488` · Dourado `#C9A962` (identidade da logo) |
| **Fonte** | Plus Jakarta Sans |
| **Tema MudBlazor** | `Web/Theme/PlateiaTheme.cs` |
| **CSS custom** | `Web/wwwroot/css/plateia.css` |
| **Logos** | `Web/wwwroot/brand/logo.png`, `logo-icon.png` |
| **Mapa de assentos** | SVG custom (estilo Ticket360), zoom/pan — **sem Seats.io** |

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
| Vitrine vendedor (logo, descrição, site) na API | ❌ pendente | ST-09 |
| Cancelamento unificado (endpoints extras) | ❌ pendente | ST-12 |

> **ST-02 (Painel do Vendedor)** é **100% frontend** — dashboard, vendas e gestão de eventos implementados; vitrine completa aguarda ST-09 no backend.

---

## 2. O que já existe no frontend (`Web/`)

### 2.1 Infraestrutura

- **Auth:** JWT em `localStorage` (`authToken`), `CustomAuthStateProvider`, roles `Admin`, `Vendedor`, `Comprador`
- **HttpClient:** BaseAddress `http://localhost:5007/` + `AuthHttpMessageHandler` (401 → logout + `/login?returnUrl=...`)
- **Estado de compra:** `PurchaseStateService` (assentos selecionados → checkout)
- **Layout:** `MainLayout.razor` — navbar Plateia, CTA "Quero Vender", menu por perfil
- **Docker:** `Web/Dockerfile` pronto

### 2.2 Páginas — status atualizado

| Rota | Arquivo | Status |
|------|---------|--------|
| `/` | `Home.razor` | ✅ API + filtro `Cancelado`, badges, CTA vendedor, demo `#if DEBUG` |
| `/login` | `Login.razor` | ✅ Redirect por perfil + link esqueci senha |
| `/cadastro` | `Cadastro.razor` | ✅ Comprador |
| `/cadastro-vendedor` | `CadastroVendedor.razor` | ✅ Auto cadastro ST-01 |
| `/esqueci-senha` | `EsqueciSenha.razor` | ✅ |
| `/redefinir-senha` | `RedefinirSenha.razor` | ✅ |
| `/comprar/{id}/ingressos` | `ComprarIngressos.razor` | ✅ SeatMap + API, até 4 assentos |
| `/checkout/{eventoId}` | `CheckoutReserva.razor` | ✅ 1–4 CPFs com máscara, cupom, `POST api/reserva/criar` |
| `/pagamento/{reservaId}` | `Pagamento.razor` | ✅ `POST api/pagamento/checkout/{reservaId}` → redirect `/reservas` |
| `/reservas` | `MinhasReservas.razor` | ✅ `GET api/reserva/minhas`, cancelamento, badges Pago/Reembolsada |
| `/perfil` | `Perfil.razor` | ✅ CRUD básico + seção vendedor (vitrine placeholder ST-09) |
| `/cupons` | `Cupons.razor` | ✅ |
| `/eventos/criar` | `Vendedor/CriarEvento.razor` | ✅ Tipo, Gratuito, Local, Descrição |
| `/eventos/editar/{id}` | `Vendedor/EditarEvento.razor` | ✅ |
| `/eventos/meus` | `Vendedor/MeusEventos.razor` | ✅ Dashboard ST-02, vendas, cancelar evento (UI) |
| `/eventos/{id}/ingressos` | `Vendedor/IngressosEvento.razor` | ✅ |
| `/admin/*` | Várias | ✅ Cupons/reservas/usuários; `/admin/cadastrar-vendedor` redireciona para `/cadastro-vendedor` |
| `/demo/assentos` | `DemoAssentos.razor` | ✅ Demo visual (mock) |
| `/evento/{id}/assentos` | `SelecionarAssentos.razor` | ✅ Redirect → `/comprar/{id}/ingressos` |

**Removidos:** `Counter.razor`, `Weather.razor` (templates Blazor).

### 2.3 Componentes SeatMap (integrados ao fluxo real)

```
Web/Components/Features/SeatMap/
├── SeatMap.razor           → Orquestrador (legenda, controles, resumo)
├── SeatMapCanvas.razor     → Viewport SVG com zoom/pan
├── SeatSvg.razor           → Renderização de assentos
├── SeatItem.razor          → Assento clicável (legado)
├── SeatMapLegend.razor
├── SeatMapSummary.razor
├── SeatMapControls.razor
├── SeatMapMapper.cs        → Ingressos API → SeatModel
├── SeatModel.cs / SeatStatus.cs / SeatBlock.cs
└── QuintaAuditorioSeatData.cs → Layout demo UNIFESO

Web/wwwroot/js/seat-map.js  → wheel/drag/pinch zoom
Web/Theme/PlateiaTheme.cs
Web/Mock/PlateiaMockData.cs
Web/Helpers/CpfFormatter.cs → máscara 000.000.000-00
Web/Services/AuthHttpMessageHandler.cs
Web/Shared/PurchaseStepper.razor
```

---

## 3. Regras de negócio que o frontend DEVE respeitar

Leia [`storytelling.md`](./storytelling.md) e [`visao.md`](./visao.md). Resumo obrigatório:

### 3.1 Perfis e autenticação

- **3 perfis:** Admin (`A1A1...`), Vendedor (`B2B2...`), Comprador (`C3C3...`)
- **Login único:** `POST /api/usuario/login` → `{ token, usuario: { cpf, nome, email, perfil } }`
- **Após login:** redirecionar por perfil:
  - Comprador → `/`
  - Vendedor → `/eventos/meus`
  - Admin → `/admin/eventos`
- **Header Authorization:** `Bearer {token}` em todas as chamadas autenticadas
- Token expira — `AuthHttpMessageHandler` trata 401 → `/login?returnUrl=...`

### 3.2 Tipos de evento

```csharp
public enum TipoEvento { Teatro = 0, Palestra = 1 }
```

| Tipo | Comportamento na compra |
|------|------------------------|
| **Teatro** | Assentos numerados VIP/Geral. Comprador escolhe assento(s) no mapa. Cada item da reserva tem `IngressoId`. |
| **Palestra** | Assentos numerados setor "Geral". Mesmo fluxo de seleção, até 4 participantes por reserva. |
| **Gratuito** (`PrecoPadrao == 0` ou `Gratuito: true`) | Pular pagamento. Após `POST /api/reserva/criar`, redirect `/reservas`. |
| **Pago** | Após criar reserva → `/pagamento/{reservaId}` → `POST /api/pagamento/checkout/{reservaId}` |

### 3.3 Reserva multi-participante (ST-04)

```json
POST /api/reserva/criar
{
  "eventoId": "guid",
  "itens": [
    { "cpfParticipante": "12345678901", "ingressoId": "guid" },
    { "cpfParticipante": "98765432100", "ingressoId": "guid" }
  ],
  "cupomCodigo": "PROMO10"
}
```

- Mínimo 1, máximo **4 itens** por reserva
- CPFs dos participantes **não precisam** ter conta no sistema
- Validar CPF no frontend (11 dígitos, máscara `000.000.000-00` via `CpfFormatter`)

### 3.4 Cancelamento de reserva (ST-05 — backend pronto)

```
DELETE /api/reserva/{id}
Authorization: Bearer {token}
```

- UI em `/reservas` com `MudDialog` + badges `Pago`, `Reembolsada`

### 3.5 Cancelamento de evento (ST-06 — backend pendente)

- UI preparada em `/eventos/meus` — botão "Cancelar evento" + dialog
- Chama `DELETE /api/evento/{id}` (comportamento de reembolso em massa depende do backend)

### 3.6 Cadastro

| Fluxo | Endpoint | Campos |
|-------|----------|--------|
| Comprador | `POST /api/usuario/CadastrarComprador` | Nome, Email, Cpf, Senha |
| Vendedor (público) | `POST /api/usuario/cadastrar-vendedor` | Cnpj, RazaoSocial, NomeFantasia, Email, Senha, Telefone |
| Admin cadastrar vendedor | ❌ Obsoleto — usar `/cadastro-vendedor` | — |

### 3.7 Esqueci minha senha (spec 180)

```
POST /api/usuario/esqueci-senha   { "email": "..." }
POST /api/usuario/redefinir-senha { "token": "...", "novaSenha": "..." }
```

Páginas: `/esqueci-senha`, `/redefinir-senha?token=...`

### 3.8 Cupons (Admin)

- AdminId **não vai na rota/body** — extraído do JWT
- Páginas Admin em `Web/Components/Pages/Admin/Cupons.razor`

---

## 4. Mapa completo de endpoints da API

Use o Swagger como fonte de verdade.

### Usuario
| Método | Rota | Auth | Uso no frontend |
|--------|------|------|-----------------|
| POST | `/api/usuario/CadastrarComprador` | — | `/cadastro` |
| POST | `/api/usuario/cadastrar-vendedor` | — | `/cadastro-vendedor` |
| POST | `/api/usuario/login` | — | `/login` |
| POST | `/api/usuario/esqueci-senha` | — | `/esqueci-senha` |
| POST | `/api/usuario/redefinir-senha` | — | `/redefinir-senha` |
| GET | `/api/usuario/Todos` | Admin | `/admin/usuarios` |
| GET | `/api/usuario/ListarUsuarioEspecifico/{cpf}` | Auth | `/perfil` |
| PUT | `/api/usuario/alterarsenha/{cpf}` | Auth | `/perfil` |
| PUT | `/api/usuario/alterarnome/{cpf}` | Auth | `/perfil` |
| PUT | `/api/usuario/alteraremail/{cpf}` | Auth | `/perfil` |
| DELETE | `/api/usuario/DeletarUsuario/{cpf}` | Auth | `/perfil` |

### Evento, Ingresso, Reserva, Pagamento, Cupom

(Mesmas rotas da versão anterior — ver Swagger.)

**Endpoints obsoletos — NÃO usar:**
- `api/Reserva/ListarPorCpf`
- `FazerReserva`, `ConfirmarPagamento`
- `api/Usuario/CadastrarVendedor/{adminId}`

---

## 5. DTOs que o frontend deve usar

Reutilize `Application/DTOs/` via referência no `Web.csproj`.

### EventoViewModel (`Web/Models/EventoViewModel.cs`)
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

---

## 6. Fluxos de UI — checklist de prioridade

### 🔴 P0 — Integrações críticas ✅ concluído

- [x] MinhasReservas → `api/reserva/minhas`, cancelamento, badges
- [x] Pagamento → `api/pagamento/checkout/{reservaId}`, redirect `/reservas`
- [x] CheckoutReserva → 1–4 CPFs, rota `/checkout/{eventoId}`, `PurchaseStateService`
- [x] Admin/CadastrarVendedor → redirect `/cadastro-vendedor`

### 🟠 P1 — Funcionalidades v2.0 ✅ concluído

- [x] Cadastro Vendedor `/cadastro-vendedor`
- [x] CriarEvento / EditarEvento — Tipo, Gratuito, Local, Descrição, preço min 0
- [x] ComprarIngressos — SeatMap integrado, até 4 assentos
- [x] Login — redirect por perfil, esqueci senha
- [x] EsqueciSenha + RedefinirSenha
- [x] CPF com máscara no checkout

### 🟡 P2 — ST-02 Painel do Vendedor ✅ parcial

- [x] Dashboard em `/eventos/meus` (eventos, vendas, receita)
- [x] Vendas via `GET api/reserva/minhas-vendas`
- [x] Cancelar evento — UI + dialog (backend ST-06 pendente)
- [x] Seção perfil vendedor — placeholder vitrine (ST-09 pendente)
- [x] Home — badges, filtro cancelado, CTA vendedor

### 🟢 P3 — Polish ✅ concluído

- [x] Tratamento 401 global (`AuthHttpMessageHandler`)
- [x] Remover Counter/Weather
- [x] SelecionarAssentos → redirect fluxo real
- [x] Pagamento redirect automático pós-sucesso
- [ ] Tratamento de erro padronizado em **todas** as páginas (parcial — priorizar novas telas)
- [ ] Responsividade mobile do SeatMap (melhorias incrementais)

---

## 7. Padrões de código exigidos

### 7.1 Estrutura de pastas

```
Web/
├── Auth/
├── Components/Features/SeatMap/
├── Components/Layout/
├── Components/Pages/Admin/ | Vendedor/
├── Components/Shared/
├── Helpers/
├── Models/
├── Services/          → PurchaseStateService, AuthHttpMessageHandler
├── Theme/PlateiaTheme.cs
└── Program.cs
```

### 7.2 Autenticação

Preferir `[Authorize(Roles = "...")]` + `<AuthorizeView>`. O `AuthHttpMessageHandler` centraliza 401.

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

### 7.4 UI — MudBlazor + Plateia

- Usar `PlateiaTheme.Theme` no `MainLayout`
- Marca na navbar: **Plateia** (`brand/logo.png`)
- CTA accent ciano (teal) para "Quero Vender"; dourado para destaques premium (gratuito, assento selecionado)
- Ícones: `Icons.Material.Filled.*`

---

## 8. Fluxo de compra completo (implementado)

```mermaid
flowchart TD
    A[Home - lista eventos] --> B{Logado?}
    B -->|Não| C[/login?returnUrl=...]
    B -->|Sim| D[/comprar/eventoId/ingressos]
    D --> E[SeatMap - selecionar até 4 assentos]
    E --> F[/checkout/eventoId - CPF por participante]
    F --> G{Aplicar cupom?}
    G --> H[POST /api/reserva/criar]
    H --> I{Evento gratuito?}
    I -->|Sim| J[/reservas]
    I -->|Não| K[/pagamento/reservaId]
    K --> L[POST /api/pagamento/checkout/reservaId]
    L --> J
```

---

## 9. Como rodar e testar

```bash
# Terminal 1 — API
cd Api && dotnet run

# Terminal 2 — Frontend
cd Web && dotnet run
```

### Checklist de teste manual

- [ ] Comprador: cadastro → login → comprar Teatro → pagar → ver reserva → cancelar
- [ ] Comprador: Palestra com 3 CPFs diferentes
- [ ] Comprador: evento gratuito — pula pagamento
- [ ] Comprador: cupom no checkout
- [ ] Vendedor: auto cadastro → criar Teatro/Palestra → ver vendas → cancelar evento
- [ ] Admin: CRUD cupons, ver reservas
- [ ] Esqueci senha → e-mail
- [ ] SeatMap reflete status real dos ingressos
- [ ] Token expirado → redirect login com returnUrl

---

## 10. O que NÃO fazer

- ❌ Não criar endpoints novos no backend
- ❌ Não passar `AdminId` em rotas de cupom
- ❌ Não usar endpoints obsoletos (ver lista acima)
- ❌ Não usar Seats.io — mapa é SVG custom
- ❌ Não permitir cupom em evento gratuito
- ❌ Não permitir mais de 4 participantes por reserva
- ❌ Não hardcodar JWT ou secrets
- ❌ Não quebrar o Docker build

---

## 11. Entregáveis esperados

1. ✅ P0 e P1 funcionando contra API v2.0
2. ✅ SeatMap integrado ao fluxo real de compra
3. ✅ Painel do Vendedor ST-02 (vitrine completa aguarda ST-09 backend)
4. ✅ Fluxo de auth completo
5. ✅ UI Plateia + MudBlazor + identidade visual
6. ✅ Zero chamadas a endpoints obsoletos

### Pendências futuras (fora do escopo frontend imediato)

- Backend ST-06: cancelamento de evento com reembolso em massa
- Backend ST-09: API para logo, descrição e site do vendedor
- E-mails ainda citam "SoldOut Tickets" — alinhar no backend/spec 180

---

> **Última atualização:** 27/06/2026 · Marca **Plateia** · Branch `frontend`
