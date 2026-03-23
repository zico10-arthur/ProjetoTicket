using Infraestructure.Repository;
using Domain.Interface;
using Application.Service;
using Application.Interfaces;
using Api.Middlewares;
using Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Defina a política
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTudo",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// ... (entre o builder.Build() e o app.Run())

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICupomRepository, CupomRepository>();
builder.Services.AddScoped<ICupomService, CupomService>();

builder.Services.AddScoped<ConnectionFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    return new ConnectionFactory(connectionString!);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// 2. Ative a política
app.UseCors("PermitirTudo");

//app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.MapControllers(); 

app.Run();