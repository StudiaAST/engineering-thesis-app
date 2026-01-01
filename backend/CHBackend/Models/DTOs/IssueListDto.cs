namespace CHBackend.Models.DTOs
{
    public class IssueListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public int? ContractorId { get; set; }
        public string? ContractorName { get; set; }

        public string? PhotoUrl { get; set; }
    }
}
