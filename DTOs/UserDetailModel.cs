using System.Net;
using UserManager.Models;

namespace UserManager.DTOs
{
    public class UserDetailModel
    {
        public int Id { get; set; }
        public string Username { get; set; }=string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string RoleTitle { get; set; } = string.Empty;
        public int RoleId { get; set; } =999999;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; }= string.Empty;
        public short Status { get; set; } = 2;
        public CompanyInformation? CompanyInfo { get; set; }
        public Address? Address { get; set; }
        public Agent? Agent { get; set; }



    }
}
