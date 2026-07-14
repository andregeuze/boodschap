using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boodschap.Features.Nutrition.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    EnergyKcal = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Protein = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Fat = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Fiber = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Foods_Name",
                table: "Foods",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Foods");
        }
    }
}
