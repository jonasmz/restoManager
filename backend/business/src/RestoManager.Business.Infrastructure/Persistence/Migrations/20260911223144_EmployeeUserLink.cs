using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestoManager.Business.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "employees",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_id",
                table: "employees");
        }
    }
}
