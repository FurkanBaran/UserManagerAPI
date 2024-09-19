using System.ComponentModel.DataAnnotations;

namespace UserManager.DTOs
{
    public class RegisterModel
    {
        public required string UserName { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public string Adress { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? AgentId { get; set; }
        public string? CompanyId { get; set; }

        public bool AgentPermission { get; set; }
        public required string Password { get; set; }
    }
}
