using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Headers;
using CHFrontend.Services;

namespace CHFrontend.Helpers
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly NavigationManager _navigationManager;
        private readonly IServiceProvider _serviceProvider;

        // Wstrzykujemy ILocalStorageService zamiast IJSRuntime
        public AuthMessageHandler(
            ILocalStorageService localStorage,
            NavigationManager navigationManager,
            IServiceProvider serviceProvider)
        {
            _localStorage = localStorage;
            _navigationManager = navigationManager;
            _serviceProvider = serviceProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);
            // 3. GLOBALNA OBSŁUGA 401
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Jeśli zapytanie dotyczyło samego logowania lub refreshu, nie próbuj odświeżać ponownie (pętla)
                if (request.RequestUri.AbsolutePath.Contains("login") || request.RequestUri.AbsolutePath.Contains("refresh-token"))
                {
                    return response;
                }

                // Pobieramy AuthService dynamicznie, żeby uniknąć pętli zależności
                var authService = _serviceProvider.GetRequiredService<AuthService>();

                // 4. Próba odświeżenia tokena
                var newToken = await authService.RefreshTokenAsync();

                if (!string.IsNullOrEmpty(newToken))
                {
                    // 5. Sukces! Podmieniamy token w nagłówku i ponawiamy zapytanie
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                    // Ważne: W Blazor/HttpClient czasem trzeba zresetować odpowiedź przed ponowieniem
                    response.Dispose();
                    return await base.SendAsync(request, cancellationToken);
                }
                else
                {
                    // 6. Refresh się nie udał -> Wyloguj użytkownika
                    await authService.Logout();
                    _navigationManager.NavigateTo("/login");
                }
            }

            return response;
        }
    }
}
