using Microsoft.AspNetCore.Identity;

namespace UserManager.Models
{
    public class User : IdentityUser<int> 
    {

        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public int? AddressId { get; set; }
        public int RoleId { get; set; }
        public int? AgentId { get; set; }
        public string? CompanyId { get; set; }
        public bool AgentPermission { get; set; }
        public short Status { get; set; } = 2;

    }
}
