using System.ComponentModel.DataAnnotations;

namespace CHBackend.Models.DTOs
{
    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool IsLocked { get; set; }
        public string Role { get; set; }
    }

    public class AdminResetPasswordDto
    {
        [Required]
        [MinLength(6, ErrorMessage = "Hasło musi mieć minimum 6 znaków")]
        public string NewPassword { get; set; }
    }
}
