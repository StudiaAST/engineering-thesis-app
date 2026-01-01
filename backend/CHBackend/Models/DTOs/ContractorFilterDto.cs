namespace CHBackend.Models.DTOs
{
    public class ContractorFilterDto
    {
        public string? Name { get; set; }
        public int? Id { get; set; }
        public string? Status { get; set; }
        public bool? HasIssues { get; set; }
        public string SortBy { get; set; } = "name";
    }
}
