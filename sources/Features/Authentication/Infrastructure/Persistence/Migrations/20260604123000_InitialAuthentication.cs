using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boodschap.Features.Authentication.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class InitialAuthentication : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "LocalUsers",
				columns: table => new
				{
					Id = table.Column<int>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Username = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					NormalizedUsername = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					PasswordHash = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
					IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
					CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_LocalUsers", x => x.Id);
				});

			migrationBuilder.CreateIndex(
				name: "IX_LocalUsers_NormalizedUsername",
				table: "LocalUsers",
				column: "NormalizedUsername",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "LocalUsers");
		}
	}
}