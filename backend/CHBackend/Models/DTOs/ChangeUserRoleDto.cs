using System.ComponentModel.DataAnnotations;

namespace CHBackend.Models.DTOs
{
    public class ChangeUserRoleDto
    {
        [Required]
        public string NewRole { get; set; }

        // Opcjonalne ID wykonawcy (tylko jeśli NewRole == "Contractor")
        public int? ContractorId { get; set; }
    }
}
