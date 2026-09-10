using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestoManager.Business.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecipeItemPublicVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                table: "recipe_items",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_public",
                table: "recipe_items");
        }
    }
}
