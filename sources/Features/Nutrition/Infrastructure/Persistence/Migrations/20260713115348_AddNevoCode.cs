using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boodschap.Features.Nutrition.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNevoCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NevoCode",
                table: "Foods",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_NevoCode",
                table: "Foods",
                column: "NevoCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Foods_NevoCode",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "NevoCode",
                table: "Foods");
        }
    }
}
