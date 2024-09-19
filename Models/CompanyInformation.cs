using System.ComponentModel.DataAnnotations;

namespace UserManager.Models
{
    public class CompanyInformation
    {
        public string? Name { get; set; }
        [Key]
        public required string IATA { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Address { get; set; }
    }

}
