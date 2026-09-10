using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestoManager.Business.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase11CartaPublica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_key",
                table: "menu_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_slug",
                table: "branches",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_branches_public_slug",
                table: "branches",
                column: "public_slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_branches_public_slug",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "image_key",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "public_slug",
                table: "branches");
        }
    }
}
