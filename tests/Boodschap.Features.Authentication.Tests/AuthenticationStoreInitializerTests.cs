using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Authentication.Tests;

public sealed class AuthenticationStoreInitializerTests
{
	[Fact]
	public async Task InitializeAsync_AppliesMigrationsToNewStore()
	{
		var databasePath = Path.Combine(Path.GetTempPath(), $"boodschap-auth-{Guid.NewGuid():N}.db");
		var connectionString = CreateConnectionString(databasePath);

		try
		{
			using (var serviceProvider = BuildServiceProvider(connectionString))
			{
				await AuthenticationStoreInitializer.InitializeAsync(serviceProvider);

				await using var scope = serviceProvider.CreateAsyncScope();
				var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AuthenticationDbContext>>();
				await using var dbContext = await dbContextFactory.CreateDbContextAsync();

				Assert.Empty(await dbContext.LocalUsers.ToListAsync());
				Assert.NotEmpty(await dbContext.Database.GetAppliedMigrationsAsync());
				Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
			}
		}
		finally
		{
			if (File.Exists(databasePath))
			{
				File.Delete(databasePath);
			}
		}
	}

	private static ServiceProvider BuildServiceProvider(string connectionString)
	{
		var services = new ServiceCollection();
		services.AddDbContextFactory<AuthenticationDbContext>(options => options.UseSqlite(
			connectionString,
			sqlite => sqlite.MigrationsHistoryTable(AuthenticationDbContext.MigrationsHistoryTableName)));

		return services.BuildServiceProvider();
	}

	private static string CreateConnectionString(string databasePath)
	{
		return new SqliteConnectionStringBuilder
		{
			DataSource = databasePath,
			Pooling = false
		}.ToString();
	}
}