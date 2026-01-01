using System.Text.Json.Serialization;

namespace CHBackend.Models;

public class Contractor
{
    public int Id { get; set; }
    public string CompanyName { get; set; }
    public string ContactInfo { get; set; }
    public string Status { get; set; }

    [JsonIgnore]
    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();

    [JsonIgnore]
    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
