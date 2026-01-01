namespace CHBackend.Models.DTOs
{
    public class ContractDetailsDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }

        public int ContractorId { get; set; }
        public Contractor Contractor { get; set; }
    }
}
