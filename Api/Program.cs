using Infraestructure.Repository;
using Domain.Interface;
using Application.Service;
using Application.Interfaces;
using Api.Middlewares;
using Infrastructure.Database;
using Application.Mappings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // ADICIONE ISSO

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddAutoMapper(typeof(UsuarioProfile));



builder.Services.AddScoped<ConnectionFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    return new ConnectionFactory(connectionString!);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Front", policy =>
        policy.WithOrigins("http://localhost:5057")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Front");

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.MapControllers(); 

app.Run();