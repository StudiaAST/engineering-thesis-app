using System.ComponentModel.DataAnnotations;

namespace CHBackend.Models.DTOs
{
    public class ContractDto
    {
        [Required]
        public string ContractNumber { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required]
        public int ContractorId { get; set; }

        [Required]
        public string Status { get; set; }
    }
}
