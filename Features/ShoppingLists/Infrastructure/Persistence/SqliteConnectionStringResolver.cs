using Microsoft.Data.Sqlite;

namespace Boodschap.Features.ShoppingLists.Infrastructure.Persistence;

public static class SqliteConnectionStringResolver
{
	private const string DefaultConnectionString = "Data Source=App_Data/boodschap.db";

	public static string Normalize(string? connectionString, string basePath)
	{
		var builder = CreateBuilder(connectionString);

		if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:" || Uri.IsWellFormedUriString(builder.DataSource, UriKind.Absolute))
		{
			return builder.ToString();
		}

		if (!Path.IsPathRooted(builder.DataSource))
		{
			builder.DataSource = Path.GetFullPath(builder.DataSource, basePath);
		}

		var directory = Path.GetDirectoryName(builder.DataSource);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		return builder.ToString();
	}

	private static SqliteConnectionStringBuilder CreateBuilder(string? connectionString)
	{
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return new SqliteConnectionStringBuilder(DefaultConnectionString);
		}

		var trimmedConnectionString = connectionString.Trim();
		if (!trimmedConnectionString.Contains('='))
		{
			return new SqliteConnectionStringBuilder
			{
				DataSource = trimmedConnectionString
			};
		}

		return new SqliteConnectionStringBuilder(trimmedConnectionString);
	}
}