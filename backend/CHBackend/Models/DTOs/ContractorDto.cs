namespace CHBackend.Models.DTOs
{
    public class ContractorDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactInfo { get; set; }
        public string Status { get; set; }
    }

    public class ContractorUpdateDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactInfo { get; set; }
        public string Status { get; set; }
    }
}
