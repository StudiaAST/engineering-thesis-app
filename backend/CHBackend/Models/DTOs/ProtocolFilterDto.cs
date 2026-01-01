namespace CHBackend.Models.DTOs
{
    public class ProtocolFilterDto
    {
        public int? Id { get; set; }
        public string? ProtocolNumber { get; set; } // Wyszukiwanie po nazwie/numerze
        public string? Area { get; set; }
        public string? State { get; set; } // Open/Closed
        public string? Type { get; set; }
        public string SortBy { get; set; } = "date";
    }
}