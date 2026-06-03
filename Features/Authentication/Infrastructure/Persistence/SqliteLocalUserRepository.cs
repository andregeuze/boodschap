using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Microsoft.Data.Sqlite;

namespace Boodschap.Features.Authentication.Infrastructure.Persistence;

public sealed class SqliteLocalUserRepository(AuthenticationStoreConfiguration configuration) : ILocalUserRepository
{
	public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
	{
		await using var connection = new SqliteConnection(configuration.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT COUNT(*) FROM \"LocalUsers\";";

		return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
	}

	public async Task<LocalUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
	{
		await using var connection = new SqliteConnection(configuration.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT "Id", "Username", "NormalizedUsername", "PasswordHash", "IsAdmin", "CreatedUtc"
			FROM "LocalUsers"
			WHERE "Id" = $id
			LIMIT 1
			""";
		command.Parameters.AddWithValue("$id", id);

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		if (!await reader.ReadAsync(cancellationToken))
		{
			return null;
		}

		return MapUser(reader);
	}

	public async Task<LocalUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
	{
		await using var connection = new SqliteConnection(configuration.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			SELECT "Id", "Username", "NormalizedUsername", "PasswordHash", "IsAdmin", "CreatedUtc"
			FROM "LocalUsers"
			WHERE "NormalizedUsername" = $normalizedUsername
			LIMIT 1
			""";
		command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		if (!await reader.ReadAsync(cancellationToken))
		{
			return null;
		}

		return MapUser(reader);
	}

	public async Task<LocalUser> CreateAsync(LocalUser user, CancellationToken cancellationToken = default)
	{
		await using var connection = new SqliteConnection(configuration.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			INSERT INTO "LocalUsers" ("Username", "NormalizedUsername", "PasswordHash", "IsAdmin", "CreatedUtc")
			VALUES ($username, $normalizedUsername, $passwordHash, $isAdmin, $createdUtc);
			SELECT last_insert_rowid();
			""";
		command.Parameters.AddWithValue("$username", user.Username);
		command.Parameters.AddWithValue("$normalizedUsername", user.NormalizedUsername);
		command.Parameters.AddWithValue("$passwordHash", user.PasswordHash);
		command.Parameters.AddWithValue("$isAdmin", user.IsAdmin);
		command.Parameters.AddWithValue("$createdUtc", user.CreatedUtc.ToString("O"));

		var createdId = (long)(await command.ExecuteScalarAsync(cancellationToken)
			?? throw new InvalidOperationException("The authentication store did not return a new user id."));

		user.Id = checked((int)createdId);
		return user;
	}

	public async Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken = default)
	{
		await using var connection = new SqliteConnection(configuration.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = """
			UPDATE "LocalUsers"
			SET "PasswordHash" = $passwordHash
			WHERE "Id" = $id;
			""";
		command.Parameters.AddWithValue("$passwordHash", passwordHash);
		command.Parameters.AddWithValue("$id", userId);

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	private static LocalUser MapUser(SqliteDataReader reader)
	{
		return new LocalUser
		{
			Id = reader.GetInt32(0),
			Username = reader.GetString(1),
			NormalizedUsername = reader.GetString(2),
			PasswordHash = reader.GetString(3),
			IsAdmin = reader.GetBoolean(4),
			CreatedUtc = DateTimeOffset.Parse(reader.GetString(5))
		};
	}
}