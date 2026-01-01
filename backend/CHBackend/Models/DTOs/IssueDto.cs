using System.ComponentModel.DataAnnotations;

namespace CHBackend.Models.DTOs
{
    public class IssueDto
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public string Status { get; set; }

        [Required]
        public int? ContractorId { get; set; }
    }

    public class IssueUpdateDto
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public string Status { get; set; }

        [Required]
        public int? ContractorId { get; set; }
    }
}