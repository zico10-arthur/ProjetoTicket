var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<Application.Mappings.EventoProfile>());
builder.Services.AddSingleton(new Infrastructure.Database.ConnectionFactory(builder.Configuration.GetConnectionString("DefaultConnection")!));
builder.Services.AddScoped<Application.Interfaces.IEventoService, Application.Services.EventoService>();
builder.Services.AddScoped<Infrastructure.Interfaces.IEventoRepository, Infraestructure.Repositories.EventoRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<Api.Middlewares.GlobalExceptionHandlerMiddleware>();


app.UseHttpsRedirection();
app.MapControllers();

app.Run();