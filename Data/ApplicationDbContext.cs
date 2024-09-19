using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using UserManager.Models;
using Microsoft.EntityFrameworkCore;

namespace UserManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<CompanyInformation> CompanyInformations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Roller için varsayılan veriler ekleniyor
            builder.Entity<Role>().HasData(
                new Role { Id = 10, Title = "Admin", HasAgentPermission = true },
                new Role { Id = 102, Title = "Manager", HasAgentPermission = true },
                new Role { Id = 1021, Title = "User", HasAgentPermission = false }
            );
        }
    }


}
