using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManager.Migrations
{
    /// <inheritdoc />
    public partial class AgentPermissionEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "agentPermission",
                table: "AspNetUsers",
                newName: "AgentPermission");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AgentPermission",
                table: "AspNetUsers",
                newName: "agentPermission");
        }
    }
}
