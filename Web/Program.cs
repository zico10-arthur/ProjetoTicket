using MudBlazor.Services;
using Web.Components; // Certifique-se que o namespace da sua pasta Components está correto

var builder = WebApplication.CreateBuilder(args);

// 1. Adiciona suporte para componentes Razor e Interatividade no Servidor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Adiciona os serviços do MudBlazor (Essencial para o Snackbar e Dialogs)
builder.Services.AddMudServices();

// 3. Configura o HttpClient para falar com a API
builder.Services.AddScoped(sp => 
{
    var handler = new HttpClientHandler();
    // Drible do SSL para desenvolvimento local
    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

    return new HttpClient(handler) 
    { 
        // Verifique se sua API está na 5000 (http) ou 7200 (https)
        BaseAddress = new Uri("http://localhost:5007/") 
    };
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
