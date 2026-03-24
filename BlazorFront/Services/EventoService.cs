using System.Net.Http.Json;
using BlazorFront.Models;

namespace BlazorFront.Services;

public class EventoService
{
    private readonly HttpClient _http;

    public EventoService(HttpClient http)
    {
        _http = http;
    }

    private async Task VerificarRespostaAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var conteudo = await response.Content.ReadAsStringAsync();
            try
            {
                var erro = System.Text.Json.JsonSerializer.Deserialize<ErroResponse>(conteudo,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                throw new Exception(erro?.Message ?? "Erro desconhecido.");
            }
            catch (System.Text.Json.JsonException)
            {
                throw new Exception(conteudo);
            }
        }
    }

    public async Task<List<EventoModel>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<EventoModel>>("api/evento") ?? [];

    public async Task<EventoModel?> GetByIdAsync(Guid id) =>
        await _http.GetFromJsonAsync<EventoModel>($"api/evento/{id}");

    public async Task CreateAsync(EventoModel evento)
    {
        var response = await _http.PostAsJsonAsync("api/evento", evento);
        await VerificarRespostaAsync(response);
    }

    public async Task UpdateAsync(Guid id, EventoModel evento)
    {
        var response = await _http.PutAsJsonAsync($"api/evento/{id}", evento);
        await VerificarRespostaAsync(response);
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/evento/{id}");
        await VerificarRespostaAsync(response);
    }

    private record ErroResponse(int StatusCode, string Message);
}
