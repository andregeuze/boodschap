using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boodschap.Features.ShoppingLists.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingListUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ShoppingLists",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ShoppingLists");
        }
    }
}
