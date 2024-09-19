namespace UserManager.DTOs
{
    public class UserListFilterModel 
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public int? Status { get; set; }
        public short? RoleId { get; set; }
        public int? UserId { get; set; }
        public int PageIndex { get; set; }
        public int PageItemCount { get; set; }
    }

}
