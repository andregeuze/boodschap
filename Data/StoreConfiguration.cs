using Microsoft.Data.Sqlite;

namespace Boodschap.Data;

public static class StoreConfiguration
{
	private const string DefaultConnectionString = "Data Source=App_Data/boodschap.db";

	public static string NormalizeSqliteConnectionString(string? connectionString, string basePath)
	{
		var sqliteConnectionString = string.IsNullOrWhiteSpace(connectionString) ? DefaultConnectionString : connectionString;
		var builder = new SqliteConnectionStringBuilder(sqliteConnectionString);

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
}