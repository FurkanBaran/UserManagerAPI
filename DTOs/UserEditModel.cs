namespace UserManager.DTOs
{
    public class UserEditModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public short? Status { get; set; }
        public int? RoleId { get; set; }
        public int? AgentId { get; set; }
        public string? CompanyId { get; set; }
        public bool? AgentPermission { get; set; }
    }
}
