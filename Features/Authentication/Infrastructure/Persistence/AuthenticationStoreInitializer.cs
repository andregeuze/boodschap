using Microsoft.Data.Sqlite;

namespace Boodschap.Features.Authentication.Infrastructure.Persistence;

public static class AuthenticationStoreInitializer
{
	public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
	{
		await using var scope = services.CreateAsyncScope();
		var configuration = scope.ServiceProvider.GetRequiredService<AuthenticationStoreConfiguration>();

		await using var connection = new SqliteConnection(configuration.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			CREATE TABLE IF NOT EXISTS "LocalUsers" (
			    "Id" INTEGER NOT NULL CONSTRAINT "PK_LocalUsers" PRIMARY KEY AUTOINCREMENT,
			    "Username" TEXT NOT NULL,
			    "NormalizedUsername" TEXT NOT NULL,
			    "PasswordHash" TEXT NOT NULL,
			    "IsAdmin" INTEGER NOT NULL DEFAULT 0,
			    "CreatedUtc" TEXT NOT NULL
			);

			CREATE UNIQUE INDEX IF NOT EXISTS "IX_LocalUsers_NormalizedUsername" ON "LocalUsers" ("NormalizedUsername");
			""";
		await command.ExecuteNonQueryAsync(cancellationToken);

		if (!await HasColumnAsync(connection, "LocalUsers", "IsAdmin", cancellationToken))
		{
			await using var addColumnCommand = connection.CreateCommand();
			addColumnCommand.CommandText = "ALTER TABLE \"LocalUsers\" ADD COLUMN \"IsAdmin\" INTEGER NOT NULL DEFAULT 0;";
			await addColumnCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		await EnsureAdminBootstrapAsync(connection, cancellationToken);
	}

	private static async Task<bool> HasColumnAsync(SqliteConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			if (string.Equals(reader.GetString(1), columnName, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static async Task EnsureAdminBootstrapAsync(SqliteConnection connection, CancellationToken cancellationToken)
	{
		await using var countCommand = connection.CreateCommand();
		countCommand.CommandText = "SELECT COUNT(*) FROM \"LocalUsers\";";
		var userCount = (long)(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
		if (userCount == 0)
		{
			return;
		}

		await using var adminCountCommand = connection.CreateCommand();
		adminCountCommand.CommandText = "SELECT COUNT(*) FROM \"LocalUsers\" WHERE \"IsAdmin\" = 1;";
		var adminCount = (long)(await adminCountCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
		if (adminCount > 0)
		{
			return;
		}

		await using var promoteCommand = connection.CreateCommand();
		promoteCommand.CommandText = """
			UPDATE "LocalUsers"
			SET "IsAdmin" = 1
			WHERE "Id" = (
			    SELECT "Id"
			    FROM "LocalUsers"
			    ORDER BY "CreatedUtc", "Id"
			    LIMIT 1
			);
			""";
		await promoteCommand.ExecuteNonQueryAsync(cancellationToken);
	}
}