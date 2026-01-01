namespace CHBackend.Models.DTOs
{
    public class IssueFilterDto
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public int? ContractorId { get; set; }
        public string SortBy { get; set; } = "title";
    }
}
