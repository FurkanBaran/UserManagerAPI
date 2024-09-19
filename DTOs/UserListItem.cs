namespace UserManager.DTOs
{
    public class UserListItem
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string RoleTitle { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }

        public required short Status { get; set; }
        public bool CanView { get; set; } = true;
        public bool CanDelete { get; set; }
        public bool CanEdit { get; set; }
        public bool CanApprove { get; set; }
    }

}
