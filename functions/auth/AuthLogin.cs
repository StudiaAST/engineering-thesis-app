using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Net;

public class AuthLogin
{
    private readonly ILogger _logger;

    public AuthLogin(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AuthLogin>();
    }

    // ===== DTO zgodne z FRONTENDEM =====
    public record LoginRequest(string? UserName, string? Password);
    public record LoginResponse(string Token, string RefreshToken, bool RequiresPasswordChange);

    [Function("AuthLogin")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        // 1️⃣ Odczyt body
        var body = await req.ReadAsStringAsync();
        LoginRequest? login = null;

        try
        {
            login = System.Text.Json.JsonSerializer.Deserialize<LoginRequest>(
                body,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }
        catch
        {
            // obsłużymy niżej
        }

        var username = login?.UserName?.Trim();
        var password = login?.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Missing username or password.");
            return bad;
        }

        // 2️⃣ MVP auth – twardy admin
        if (!username.Equals("admin@constructhub.local", StringComparison.OrdinalIgnoreCase)
            || password != "Admin!12345")
        {
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorized.WriteStringAsync("Invalid credentials.");
            return unauthorized;
        }

        // 3️⃣ JWT
        var signingKey = Environment.GetEnvironmentVariable("JWT__SigningKey");
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
        {
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("JWT signing key missing or too short (min 32).");
            return err;
        }

        var issuer = Environment.GetEnvironmentVariable("JWT__Issuer") ?? "ConstructHub";
        var audience = Environment.GetEnvironmentVariable("JWT__Audience") ?? "ConstructHubClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        // 4️⃣ Odpowiedź ZGODNA Z FRONTENDEM
        var refreshToken = Guid.NewGuid().ToString("N"); // MVP – fake refresh
        var response = new LoginResponse(
            Token: jwt,
            RefreshToken: refreshToken,
            RequiresPasswordChange: false
        );

        var ok = req.CreateResponse(HttpStatusCode.OK);
        ok.Headers.Add("Content-Type", "application/json");
        await ok.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(response));

        return ok;
    }
}
