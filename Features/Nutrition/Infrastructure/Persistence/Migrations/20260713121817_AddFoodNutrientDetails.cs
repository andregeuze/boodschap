using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boodschap.Features.Nutrition.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodNutrientDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnglishFoodGroup",
                table: "Foods",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "Foods",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FoodGroup",
                table: "Foods",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NevoVersion",
                table: "Foods",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Quantity",
                table: "Foods",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FoodNutrientDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FoodId = table.Column<int>(type: "INTEGER", nullable: false),
                    NutrientGroup = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    ComponentGroup = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    NutrientCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NutrientName = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Component = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    RawValue = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TraceFortified = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodNutrientDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodNutrientDetails_Foods_FoodId",
                        column: x => x.FoodId,
                        principalTable: "Foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodNutrientDetails_FoodId_NutrientCode",
                table: "FoodNutrientDetails",
                columns: new[] { "FoodId", "NutrientCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodNutrientDetails");

            migrationBuilder.DropColumn(
                name: "EnglishFoodGroup",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "FoodGroup",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "NevoVersion",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Foods");
        }
    }
}
