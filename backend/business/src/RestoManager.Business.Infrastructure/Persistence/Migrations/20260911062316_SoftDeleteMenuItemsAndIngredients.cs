using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestoManager.Business.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteMenuItemsAndIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "menu_items",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "ingredients",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "ingredients");
        }
    }
}
