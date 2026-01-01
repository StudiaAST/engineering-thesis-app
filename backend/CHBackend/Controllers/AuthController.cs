using CHBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var user = new AppUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName,
            ContractorId = null
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, "User"); //Przypisanie domyślnej roli "User"
        return Ok("Użytkownik został zarejestrowany.");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user == null)
            return Unauthorized("Nieprawidłowy e-mail lub hasło.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (result.IsLockedOut)
        {
            return StatusCode(403, "Twoje konto zostało zablokowane. Skontaktuj się z administratorem.");
        }

        if (!result.Succeeded)
            return Unauthorized("Nieprawidłowy e-mail lub hasło.");

        var accessToken = await CreateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        _ = int.TryParse(_configuration["Jwt:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenValidityInDays == 0 ? 7 : refreshTokenValidityInDays); // Domyślnie 7 dni
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            Token = accessToken,           // Access Token do zapytań
            RefreshToken = refreshToken,   // Refresh Token do odnawiania
            Expiration = DateTime.UtcNow.AddMinutes(_configuration.GetValue<double>("Jwt:ExpireMinutes", 15)),
            RequiresPasswordChange = user.MustChangePassword
        });

    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return BadRequest("Nie znaleziono użytkownika. Zaloguj się ponownie.");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest($"Nie udało się zmienić hasła: {GetReadableErrorMessage(result)}");
        }

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        return Ok("Hasło zostało pomyślnie zmienione.");
    }

    [HttpPost("force-change-password")]
    [Authorize] // Użytkownik jest zalogowany (bo właśnie przeszedł logowanie)
    public async Task<IActionResult> ForceChangePassword([FromBody] ForceChangePasswordDto model)
    {
        // 1. Pobieramy zalogowanego użytkownika (z tokena, który dostał przy logowaniu)
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized("Błąd sesji. Zaloguj się ponownie.");

        // 2. Generujemy token resetowania (to pozwala pominąć stare hasło)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // 3. Ustawiamy nowe hasło
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest($"Nie udało się zmienić hasła: {GetReadableErrorMessage(result)}");
        }

        // 4. Wyłączamy wymuszenie zmiany hasła, żeby już nie męczyć użytkownika
        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Ok("Hasło zostało pomyślnie zmienione.");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenApiDto tokenModel)
    {
        if (tokenModel is null)
            return BadRequest("Błędne żądanie klienta (Invalid Client Request)");

        string accessToken = tokenModel.AccessToken;
        string refreshToken = tokenModel.RefreshToken;

        var principal = GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
            return BadRequest("Nieprawidłowy Access Token lub Refresh Token");

        // Pobieramy nazwę użytkownika z tokena
        var username = principal.Identity?.Name;

        var user = await _userManager.FindByNameAsync(username);

        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow || await _userManager.IsLockedOutAsync(user))
        {
            return BadRequest("Konto zablokowane lub sesja wygasła.");
        }

        var newAccessToken = await CreateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }

    private string GetReadableErrorMessage(IdentityResult result)
    {
        // Wybiera opisy błędów i łączy je znakiem nowej linii lub przecinkiem
        // Np.: "Hasło musi mieć cyfrę. Hasło jest za krótkie."
        return string.Join(" ", result.Errors.Select(e => e.Description));
    }

    private async Task<string> CreateJwtToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName), // Ważne dla User.Identity.Name
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Dodajemy ID wykonawcy do tokena, jeśli użytkownik jest przypisany do firmy
        if (user.ContractorId.HasValue)
        {
            claims.Add(new Claim("ContractorId", user.ContractorId.Value.ToString()));
        }

        //Pobieranie i dodawanie ról
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expireMinutes = _configuration.GetValue<double>("Jwt:ExpireMinutes", 15); // Krótki czas życia!

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, // W trakcie refreshu walidujemy tylko podpis
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
            ValidateLifetime = false // <--- KLUCZOWE: Pozwalamy na wygasły token, żeby odczytać z niego kim jest user
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

}