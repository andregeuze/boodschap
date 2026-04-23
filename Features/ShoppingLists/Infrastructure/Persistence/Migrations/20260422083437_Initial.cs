using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boodschap.Features.ShoppingLists.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class Initial : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "ShoppingLists",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
					Archived = table.Column<bool>(type: "INTEGER", nullable: false),
					SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ShoppingLists", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "ShoppingListItems",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					ShoppingListId = table.Column<int>(type: "INTEGER", nullable: false),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					IsDone = table.Column<bool>(type: "INTEGER", nullable: false),
					SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ShoppingListItems", x => x.Id);
					table.ForeignKey(
						name: "FK_ShoppingListItems_ShoppingLists_ShoppingListId",
						column: x => x.ShoppingListId,
						principalTable: "ShoppingLists",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_ShoppingListItems_ShoppingListId",
				table: "ShoppingListItems",
				column: "ShoppingListId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "ShoppingListItems");

			migrationBuilder.DropTable(
				name: "ShoppingLists");
		}
	}
}