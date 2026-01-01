using CHFrontend;
using CHFrontend.Helpers;
using CHFrontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Adres API
var apiBaseUrl = "http://localhost:5150";

// 1. Rejestracja LocalStorage (niezbędne dla CustomAuthProvider)
builder.Services.AddBlazoredLocalStorage(); // <--- WAŻNE

builder.Services.AddAuthorizationCore(options =>
{
    // --- 1. UŻYTKOWNICY (Users) ---
    // Tylko Admin może cokolwiek robić z użytkownikami
    options.AddPolicy("CanReadUsers", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("CanUpdateCreateUsers", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("CanDeleteUsers", policy =>
        policy.RequireRole("Admin"));

    // --- 2. ZLECENIA (Contracts) ---
    options.AddPolicy("CanReadContracts", policy =>
        policy.RequireRole("Admin", "Manager", "Contractor"));
    options.AddPolicy("CanUpdateCreateContracts", policy =>
        policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("CanDeleteContracts", policy =>
        policy.RequireRole("Admin", "Manager"));

    // --- 3. USTERKI (Issues) ---
    options.AddPolicy("CanReadIssues", policy =>
        policy.RequireRole("Admin", "Manager", "User", "Contractor"));
    options.AddPolicy("CanUpdateCreateIssues", policy =>
        policy.RequireRole("Admin", "Manager", "Contractor"));
    options.AddPolicy("CanDeleteIssues", policy =>
        policy.RequireRole("Admin", "Manager"));

    // --- 4. WYKONAWCY (Contractors) ---
    options.AddPolicy("CanReadContractors", policy =>
        policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("CanUpdateCreateContractors", policy =>
        policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("CanDeleteContractors", policy =>
        policy.RequireRole("Admin", "Manager"));
});

// 2. Rejestracja Autoryzacji
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState(); // <--- WAŻNE: Wymagane przez <CascadingAuthenticationState> w App.razor

// 3. REJESTRACJA DOSTAWCY STANU (To naprawia błąd "There is no registered service...")
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthProvider>(); // <--- WAŻNE

// Rejestrujemy handler, który doda token JWT do zapytań
//builder.Services.AddScoped<AuthMessageHandler>();
builder.Services.AddTransient<AuthMessageHandler>();

// 3. KLIENT HTTP 1: "PublicApi" (Bez Handlera - do Login i Refresh)
builder.Services.AddHttpClient("PublicApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Tworzymy HttpClient z handlerem (token dodawany automatycznie)
builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthMessageHandler>();

// Ustawiamy ten HttpClient jako domyślny
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthorizedClient"));

// Twoje istniejące serwisy
builder.Services.AddScoped<AuthService>();



await builder.Build().RunAsync();