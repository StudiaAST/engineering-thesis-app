using System.ComponentModel.DataAnnotations;

namespace CHBackend.Models.DTOs
{
    public class ProtocolDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Data protokołu jest wymagana")]
        public DateTime ReceiptDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Nr protokołu jest wymagany")]
        public string ProtocolNumber { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime? FixDate { get; set; }
        public string State { get; set; } = "Open";
    }
}