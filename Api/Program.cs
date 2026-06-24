using Infraestructure.Repository;
using Domain.Interface;
using Application.Service;
using Application.Interfaces;
using Api.Middlewares;
using Infrastructure.Database;
using Application.Mappings;
using Infraestructure.DataBase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Hangfire;
using Hangfire.SqlServer;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Spec 120: Carregar Jwt:Key de user-secrets em desenvolvimento
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Spec 120: Validar Jwt:Key obrigatória antes de configurar autenticação
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key não configurada. Em desenvolvimento, execute:\n" +
        "  dotnet user-secrets set \"Jwt:Key\" \"<sua-chave-secreta-de-32-caracteres>\"\n" +
        "  dotnet user-secrets set \"Jwt:Issuer\" \"ProjetoTicket\"\n" +
        "  dotnet user-secrets set \"Jwt:Audience\" \"ProjetoTicket\"");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICupomRepository, CupomRepository>();
builder.Services.AddScoped<ICupomService, CupomService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<Domain.Interface.IEventoRepository, Infraestructure.Repository.EventoRepository>();
builder.Services.AddScoped<Application.Interfaces.IEventoService, Application.Service.EventoService>();
builder.Services.AddScoped<IIngressoRepository, IngressoRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IIngressoService, IngressoService>();
builder.Services.AddScoped<IPagamentoRepository, PagamentoRepository>();
builder.Services.AddScoped<IPagamentoService, PagamentoService>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<UsuarioProfile>();
    cfg.AddProfile<Application.Mappings.EventoProfile>();
    cfg.AddProfile<Application.Mappings.IngressoProfile>();
});

builder.Services.AddScoped<ConnectionFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    return new ConnectionFactory(connectionString!);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",
            NameClaimType = "email"
        };
    });

builder.Services.AddAuthorization();

// Hangfire
builder.Services.AddHangfire(config =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")!;
    config.UseSqlServerStorage(cs, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        PrepareSchemaIfNecessary = true
    });
});

builder.Services.AddHangfireServer();

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("DefaultConnection")!;
DatabaseMigration.Initialize(connectionString);

// ST-08: Seed Admin com BCrypt + Perfis
using (var scope = app.Services.CreateScope())
{
    var seederFactory = scope.ServiceProvider.GetRequiredService<ConnectionFactory>();
    DatabaseSeeder.Seed(seederFactory);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard (apenas Admin)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthorizationFilter() }
});

// Registrar recurring job de liberação de assentos
using (var hangfireScope = app.Services.CreateScope())
{
    var recurringJobManager = hangfireScope.ServiceProvider
        .GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<Api.BackgroundTasks.LiberacaoAssentosJob>(
        "liberacao-assentos-expirados",
        job => job.ExecutarAsync(default),
        Cron.Minutely);
}

app.MapControllers();

app.Run();
