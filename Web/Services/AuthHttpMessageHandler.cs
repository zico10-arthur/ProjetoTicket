using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Web.Auth;

namespace Web.Services;

public class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _js;
    private readonly AuthenticationStateProvider _authState;

    public AuthHttpMessageHandler(
        NavigationManager navigation,
        IJSRuntime js,
        AuthenticationStateProvider authState)
    {
        _navigation = navigation;
        _js = js;
        _authState = authState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");

        if (_authState is CustomAuthStateProvider custom)
            custom.MarcarUsuarioComoDeslogado();

        var returnUrl = Uri.EscapeDataString(_navigation.Uri);
        _navigation.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);

        return response;
    }
}
