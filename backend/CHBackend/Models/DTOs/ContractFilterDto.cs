using System.ComponentModel.DataAnnotations;

namespace CHBackend.Models.DTOs
{
    public class ContractFilterDto
    {
        public int Id { get; set; }

        public string? ContractNumber { get; set; }

        public string? Name { get; set; }

        public int? ContractorId { get; set; }

        public string? Status { get; set; }

        public string SortBy { get; set; } = "StartDateDesc";
    }
}
