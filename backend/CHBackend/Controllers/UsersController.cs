using CHBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public UsersController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Policy = "CanReadUsers")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);

            userDtos.Add(new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                FullName = u.FullName,
                // Sprawdzamy, czy blokada jest aktywna (data końca blokady jest w przyszłości)
                IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                Role = roles.FirstOrDefault() ?? "User"
            });
        }

            return Ok(userDtos);
    }

    [HttpPost]
    [Authorize(Policy = "CanUpdateCreateUsers")]
    public async Task<IActionResult> CreateUser([FromBody] RegisterDto model)
    {
        if (model.Role == "Contractor" && !model.ContractorId.HasValue)
        {
            return BadRequest("Dla roli 'Wykonawca' wymagane jest przypisanie firmy.");
        }

        var user = new AppUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true, // Zakładamy, że admin tworzy zweryfikowanego usera
            MustChangePassword = true,
            // Przypisujemy firmę TYLKO jeśli rola to Contractor
            ContractorId = (model.Role == "Contractor") ? model.ContractorId : null
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, model.Role);
        return Ok("Użytkownik został zarejestrowany.");
    }

    [HttpPut("{id}/toggle-status")]
    [Authorize(Policy = "CanUpdateCreateUsers")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound("Użytkownik nie istnieje.");

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            // Jest zablokowany -> ODBLOKUJ
            user.LockoutEnd = null;
        }
        else
        {
            // Jest aktywny -> ZABLOKUJ (ustaw blokadę na 1000 lat)
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(1000);
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return BadRequest("Nie udało się zmienić statusu.");

        return Ok(new { IsLocked = user.LockoutEnd.HasValue });
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = "CanUpdateCreateUsers")]
    [Authorize] // Ewentualnie [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordDto model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound("Użytkownik nie istnieje.");

        // 1. Wygeneruj token do resetu hasła (wymagany przez Identity)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // 2. Wykonaj reset hasła na to, które podał Admin
        var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (!resetResult.Succeeded)
            return BadRequest($"Nie udało się zresetować hasła: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");

        // 3. Wymuś zmianę hasła przy kolejnym logowaniu
        user.MustChangePassword = true;

        // 4. (Opcjonalnie) Wyloguj użytkownika ze wszystkich sesji (unieważnij RefreshToken)
        user.RefreshToken = null;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
            return BadRequest("Hasło zmienione, ale nie udało się ustawić flagi wymuszenia zmiany.");

        return Ok("Hasło zostało zresetowane. Użytkownik musi je zmienić przy logowaniu.");
    }

    [HttpPost("{id}/change-role")]
    [Authorize(Policy = "CanUpdateCreateUsers")] // Wymaga Admina
    public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeUserRoleDto model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound("Użytkownik nie istnieje.");

        // 1. Pobierz obecne role i usuń je (żeby user nie miał kilku ról naraz)
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // 2. Dodaj nową rolę
        var addResult = await _userManager.AddToRoleAsync(user, model.NewRole);
        if (!addResult.Succeeded) return BadRequest("Nie udało się nadać nowej roli.");

        // 3. Specjalna obsługa Wykonawcy (Contractor)
        if (model.NewRole == "Contractor")
        {
            if (!model.ContractorId.HasValue)
                return BadRequest("Dla roli Wykonawca musisz wybrać firmę.");

            user.ContractorId = model.ContractorId;
        }
        else
        {
            // Jeśli rola to nie Contractor (np. Admin lub Manager), 
            // czyścimy powiązanie z firmą, żeby nie miał dostępu do zleceń po starej znajomości
            user.ContractorId = null;
        }

        await _userManager.UpdateAsync(user);

        return Ok("Uprawnienia zostały zaktualizowane.");
    }
}