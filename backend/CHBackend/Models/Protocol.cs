using System.ComponentModel.DataAnnotations;

namespace CHBackend.Models
{
    public class Protocol
    {
        public int Id { get; set; }

        public DateTime ReceiptDate { get; set; } // Data protokołu odbioru

        [Required]
        public string ProtocolNumber { get; set; } = string.Empty; // Nr protokołu (np. "Uszkodzona antykorozja...")

        public string Type { get; set; } = string.Empty; // Rodzaj protokołu (np. OCB)

        public string Area { get; set; } = string.Empty; // Obszar (np. Stacja DRIM)

        public string DocumentNumber { get; set; } = string.Empty; // Nr dokumentacji

        public string StatusDescription { get; set; } = string.Empty; // Status (np. Odbiór z wynikiem...)

        public DateTime? FixDate { get; set; } // Data usunięcia usterek (może być pusta)

        public string State { get; set; } = "Open"; // Open/Closed
    }
}