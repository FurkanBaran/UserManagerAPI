using Microsoft.AspNetCore.Identity;
namespace UserManager.Models
{

    public class Role : IdentityRole<int>
    {
        public required string Title { get; set; }
        public bool HasAgentPermission { get; set; }
    }
}
