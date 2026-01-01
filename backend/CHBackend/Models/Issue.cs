using CHBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Issue
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    public string Description { get; set; }

    public string Location { get; set; }

    public string Status { get; set; }
    public int? ContractorId { get; set; }


    [ForeignKey("ContractorId")]
    [JsonIgnore]
    public virtual Contractor Contractor { get; set; }
    public ICollection<Photo> Photos { get; set; } // Kolekcja zdjęć powiązanych z Issue
}
