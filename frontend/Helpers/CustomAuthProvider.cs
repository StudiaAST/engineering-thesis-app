using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

namespace CHFrontend.Helpers
{
    public class CustomAuthProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;

        public CustomAuthProvider(ILocalStorageService localStorage, HttpClient http)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Tutaj pobieramy token. Upewnij się, że przy logowaniu zapisujesz go pod kluczem "authToken"
                var token = await _localStorage.GetItemAsync<string>("authToken");

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt")));
            }
            catch (Exception)
            {
                // ZMIANA 2: Zabezpieczenie przed crashem (błąd RenderTreeDiffBuilder)
                // Jeśli token jest uszkodzony, po prostu uznajemy użytkownika za wylogowanego
                // i czyścimy błędne dane, żeby nie blokowały aplikacji.
                await _localStorage.RemoveItemAsync("authToken");
                await _localStorage.RemoveItemAsync("refreshToken");

                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyUserAuthentication(string token)
        {
            try
            {
                var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
                var authState = Task.FromResult(new AuthenticationState(authenticatedUser));

                // To jest metoda wbudowana w Blazora - ona odświeża widok!
                NotifyAuthenticationStateChanged(authState);
            }
            catch
            {
                // Zabezpieczenie, gdyby token podany po logowaniu był błędny
                NotifyUserLogout();
            }
        }
        public void NotifyUserLogout()
        {
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));

            NotifyAuthenticationStateChanged(authState);
        }

        public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = new List<Claim>();

            foreach (var kvp in keyValuePairs)
            {
                // Sprawdzamy, czy klucz to rola (może być "role" lub pełny URI Microsoftu)
                if (kvp.Key == "role" || kvp.Key == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    // Konwertujemy wartość na string, żeby sprawdzić czy to tablica
                    var valueString = kvp.Value.ToString();

                    if (valueString.Trim().StartsWith("["))
                    {
                        // To jest tablica ról (np. ["Admin", "User"])
                        var parsedRoles = JsonSerializer.Deserialize<string[]>(valueString);

                        foreach (var parsedRole in parsedRoles)
                        {
                            // Dodajemy każdą rolę jako OSOBNY Claim
                            claims.Add(new Claim(ClaimTypes.Role, parsedRole));
                        }
                    }
                    else
                    {
                        // To jest pojedyncza rola
                        claims.Add(new Claim(ClaimTypes.Role, valueString));
                    }
                }
                else
                {
                    // Wszystkie inne claimy (email, nameid itp.)
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString()));
                }
            }
            return claims;
            //return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}