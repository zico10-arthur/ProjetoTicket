using Infraestructure.Repository;
using Domain.Interface;
using Application.Service;
using Application.Interfaces;
using Api.Middlewares;
using Infrastructure.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minha API", Version = "v1" });

    // 1. Cria o botão "Authorize" no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta maneira: Bearer {seu_token_aqui}"
    });

    // 2. Garante que o Swagger envie o token em todas as requisições que exigirem
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICupomRepository, CupomRepository>();
builder.Services.AddScoped<ICupomService, CupomService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "ProjetoTicketAPI", // Tem que ser igual ao que colocamos no UsuarioService
            ValidAudience = "ProjetoTicketWeb", // Tem que ser igual ao do UsuarioService
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperChaveSecretaDoProjetoTicket2026!!!"))
        };
    });

builder.Services.AddScoped<ConnectionFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    return new ConnectionFactory(connectionString!);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTudo", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Pegue a mesma ConnectionString que você passa para o seu ConnectionFactory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Chama o DbUp para criar o banco e as tabelas
Infrastructure.Database.DatabaseMigration.Initialize(connectionString);

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("PermitirTudo");

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.MapControllers(); 

app.UseAuthentication();
app.UseAuthorization();

app.Run();