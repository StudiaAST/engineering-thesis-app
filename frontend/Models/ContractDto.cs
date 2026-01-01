using System.ComponentModel.DataAnnotations;

namespace CHFrontend.Models
{
    public class ContractDto
    {
        [Required(ErrorMessage = "Numer zlecenia jest wymagany.")]
        public string ContractNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwa zlecenia jest wymagana.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Opis zlecenia jest wymagany.")]
        public string Description { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Wybór wykonawcy zlecenia jest wymagany.")]
        public int? ContractorId { get; set; }

        [Required(ErrorMessage = "Wybór statusu zlecenia jest wymagany.")]
        public string Status { get; set; } = string.Empty;

        // Not mapped from API, used for editing existing contracts
        public int Id { get; set; }
    }
}
