using MudBlazor.Services;
using Web.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Web.Auth;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Adiciona suporte para componentes Razor e Interatividade no Servidor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Adiciona os serviços do MudBlazor (Essencial para o Snackbar e Dialogs)
builder.Services.AddMudServices();

// 3. Configura o HttpClient para falar com a API (401 → logout + /login)
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException(
        "ApiBaseUrl não configurada. Defina em appsettings.json ou via variável de ambiente ApiBaseUrl.");

builder.Services.AddScoped<AuthHttpMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var authHandler = sp.GetRequiredService<AuthHttpMessageHandler>();
    authHandler.InnerHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };

    return new HttpClient(authHandler)
    {
        BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/")
    };
});

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Vendedor", policy => policy.RequireRole("Vendedor"));
});
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<PurchaseStateService>();

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

// 4. Configura o Pipeline de requisições
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Define que o App.razor é a raiz e aceita modo interativo
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
