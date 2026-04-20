// =============================================
// Contrato da API — SoldOut Tickets
// Minimal API com todas as rotas mapeadas
// =============================================

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ── USUÁRIO ──────────────────────────────────

// Cadastrar comprador
app.MapPost("api/Usuario/CadastrarComprador", () => { });

// Cadastrar vendedor (apenas Admin)
app.MapPost("api/Usuario/CadastrarVendedor/{id}", () => { });

// Login
app.MapPost("api/Usuario/login", () => { });

// Listar usuário específico por CPF
app.MapGet("api/Usuario/ListarUsuarioEspecifico/{cpf}", () => { });

// Listar todos os usuários (apenas Admin)
app.MapGet("api/Usuario/Todos", () => { });

// Deletar usuário por CPF
app.MapDelete("api/Usuario/DeletarUsuario/{cpf}", () => { });

// Alterar senha
app.MapPut("api/Usuario/alterarsenha/{cpf}", () => { });

// Alterar nome
app.MapPut("api/Usuario/alterarnome/{cpf}", () => { });

// Alterar e-mail
app.MapPut("api/Usuario/alteraremail/{cpf}", () => { });

// ── EVENTO ───────────────────────────────────

// Listar todos os eventos
app.MapGet("api/Evento", () => { });

// Listar eventos do vendedor logado
app.MapGet("api/Evento/meus", () => { });

// Buscar evento por ID
app.MapGet("api/Evento/{id}", () => { });

// Criar evento (apenas Vendedor)
app.MapPost("api/Evento", () => { });

// Atualizar evento (apenas Vendedor dono)
app.MapPut("api/Evento/{id}", () => { });

// Deletar evento (Vendedor dono ou Admin)
app.MapDelete("api/Evento/{id}", () => { });

// ── INGRESSO ─────────────────────────────────

// Listar ingressos de um evento
app.MapGet("api/Ingresso/eventos/{eventoId}/ingressos", () => { });

// Buscar ingresso por ID
app.MapGet("api/Ingresso/{id}", () => { });

// ── CUPOM ────────────────────────────────────

// Cadastrar cupom (apenas Admin)
app.MapPost("api/Cupom/CadastrarCupom/{id}", () => { });

// Deletar cupom (apenas Admin)
app.MapDelete("api/Cupom/DeletarCupom/{codigo}", () => { });

// Listar todos os cupons (apenas Admin)
app.MapGet("api/Cupom/ListarTodosCupons", () => { });

// Listar cupons válidos
app.MapGet("api/Cupom/ListarCuponsValidos", () => { });

// Alterar valor mínimo do cupom (apenas Admin)
app.MapMethods("api/Cupom/{codigo}/ValorMinimo", new[] { "PATCH" }, () => { });

// Alterar data de expiração do cupom (apenas Admin)
app.MapMethods("api/Cupom/{codigo}/DataExpiracao", new[] { "PATCH" }, () => { });

// Alternar status do cupom (apenas Admin)
app.MapMethods("api/Cupom/{codigo}/AlternarStatus", new[] { "PATCH" }, () => { });

// Alterar desconto do cupom (apenas Admin)
app.MapMethods("api/Cupom/{codigo}/AlterarDesconto", new[] { "PATCH" }, () => { });

// ── RESERVA ──────────────────────────────────

// Fazer reserva (apenas Comprador)
app.MapPost("api/Reserva/FazerReserva", () => { });

// Confirmar pagamento (apenas Comprador)
app.MapPost("api/Reserva/ConfirmarPagamento/{ingressoId}", () => { });

// Listar minhas reservas (apenas Comprador)
app.MapGet("api/Reserva/ListarPorCpf", () => { });

// Listar todas as reservas (apenas Admin)
app.MapGet("api/Reserva/Admin/Todas", () => { });

app.Run();
