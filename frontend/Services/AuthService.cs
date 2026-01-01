using Blazored.LocalStorage;
using CHFrontend.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace CHFrontend.Services
{
    public class AuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(
            IHttpClientFactory httpClientFactory,
            ILocalStorageService localStorage,
            AuthenticationStateProvider authStateProvider)
        {
            _httpClientFactory = httpClientFactory;
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
        }

        // DTO dopasowane do backendu: UserName + Password
        private sealed class LoginRequestDto
        {
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            // Klient bez AuthMessageHandlera (do login/refresh)
            var client = _httpClientFactory.CreateClient("PublicApi");

            var payload = new LoginRequestDto
            {
                UserName = username,
                Password = password
            };

            var response = await client.PostAsJsonAsync("api/auth/login", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var cleanError = errorContent.Trim('"');
                if (string.IsNullOrWhiteSpace(cleanError))
                    cleanError = "Wystąpił błąd logowania.";
                return new AuthResult { Success = false, ErrorMessage = cleanError };
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResultDto>();
            if (result == null)
                return new AuthResult { Success = false, ErrorMessage = "Błąd przetwarzania danych z serwera." };

            await _localStorage.SetItemAsync("authToken", result.Token);
            await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

            ((CustomAuthProvider)_authStateProvider).NotifyUserAuthentication(result.Token);

            return new AuthResult { Success = true, Data = result };
        }

        public async Task<string> RefreshTokenAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
                return null;

            var refreshDto = new RefreshTokenDto { AccessToken = token, RefreshToken = refreshToken };

            var client = _httpClientFactory.CreateClient("PublicApi");
            var response = await client.PostAsJsonAsync("api/auth/refresh-token", refreshDto);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<LoginResultDto>();
            if (result == null)
                return null;

            await _localStorage.SetItemAsync("authToken", result.Token);
            await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

            return result.Token;
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("refreshToken");
            ((CustomAuthProvider)_authStateProvider).NotifyUserLogout();
        }

        public class AuthResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public LoginResultDto Data { get; set; }
        }

        public class LoginResultDto
        {
            public string Token { get; set; }
            public string RefreshToken { get; set; }
            public bool RequiresPasswordChange { get; set; }
        }

        public class RefreshTokenDto
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
        }
    }
}
