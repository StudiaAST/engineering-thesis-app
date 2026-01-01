namespace CHBackend.Models.DTOs
{
    public class ProtocolListDto
    {
        public int Id { get; set; }
        public DateTime ReceiptDate { get; set; } // Data protokołu
        public string ProtocolNumber { get; set; } = string.Empty; // Nr protokołu
        public string Type { get; set; } = string.Empty; // Rodzaj
        public string Area { get; set; } = string.Empty; // Obszar
        public string DocumentNumber { get; set; } = string.Empty; // Nr dokumentacji
        public string StatusDescription { get; set; } = string.Empty; // Status
        public DateTime? FixDate { get; set; } // Data usunięcia usterek
        public string State { get; set; } = string.Empty; // Open/Closed
    }
}