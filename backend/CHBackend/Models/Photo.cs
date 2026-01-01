using System.Text.Json.Serialization;

namespace CHBackend.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; }  // Adres URL do zdjęcia
        public int IssueId { get; set; } // Powiązanie z Issue

        [JsonIgnore]
        public Issue Issue { get; set; } // Nawigacja do Issue
    }

}
