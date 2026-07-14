using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boodschap.Features.Nutrition.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateFoodNutrientCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodNutrientDetails_FoodId_NutrientCode",
                table: "FoodNutrientDetails");

            migrationBuilder.CreateIndex(
                name: "IX_FoodNutrientDetails_FoodId_NutrientCode",
                table: "FoodNutrientDetails",
                columns: new[] { "FoodId", "NutrientCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodNutrientDetails_FoodId_NutrientCode",
                table: "FoodNutrientDetails");

            migrationBuilder.CreateIndex(
                name: "IX_FoodNutrientDetails_FoodId_NutrientCode",
                table: "FoodNutrientDetails",
                columns: new[] { "FoodId", "NutrientCode" },
                unique: true);
        }
    }
}