var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(typeof(Application.Mappings.EventoProfile));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.Run();