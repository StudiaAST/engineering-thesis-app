using System.ComponentModel.DataAnnotations;

namespace CHFrontend.Models
{
    public class ForceChangePasswordDto
    {
        [Required(ErrorMessage = "Nowe hasło jest wymagane.")]
        [MinLength(6, ErrorMessage = "Hasło musi mieć minimum 6 znaków.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]
        [Compare("NewPassword", ErrorMessage = "Hasła muszą być identyczne.")] // <--- To jest Twój walidator
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
