using CHBackend.Models;
using Microsoft.AspNetCore.Identity;

public class AppUser : IdentityUser
{
    public string FullName { get; set; }
    public bool MustChangePassword { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public int? ContractorId { get; set; }
    public virtual Contractor? Contractor { get; set; }
}

