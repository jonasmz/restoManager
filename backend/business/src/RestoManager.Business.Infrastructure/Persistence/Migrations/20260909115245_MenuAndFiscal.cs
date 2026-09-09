using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RestoManager.Business.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MenuAndFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "menu_item_branch_availability",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    menu_item_id = table.Column<int>(type: "integer", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_item_branch_availability", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_item_branch_availability_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_menu_item_branch_availability_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_branch_availability_branch_id_menu_item_id",
                table: "menu_item_branch_availability",
                columns: new[] { "branch_id", "menu_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_branch_availability_menu_item_id",
                table: "menu_item_branch_availability",
                column: "menu_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_item_branch_availability");
        }
    }
}
