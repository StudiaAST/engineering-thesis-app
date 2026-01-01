using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CHBackend.Models
{
    public class Contract
    {
        public int Id { get; set; }
        
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

        [ForeignKey("ContractorId")]
        [JsonIgnore]
        public virtual Contractor Contractor { get; set; }
    }
}
