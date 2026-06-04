using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Authentication.Tests.Testing;

public sealed class AuthenticationSqliteTestHarness : IAsyncDisposable
{
	private readonly SqliteConnection connection;

	private AuthenticationSqliteTestHarness(
		SqliteConnection connection,
		IDbContextFactory<AuthenticationDbContext> dbContextFactory)
	{
		this.connection = connection;
		DbContextFactory = dbContextFactory;
	}

	public IDbContextFactory<AuthenticationDbContext> DbContextFactory { get; }

	public static async Task<AuthenticationSqliteTestHarness> CreateAsync()
	{
		var connection = new SqliteConnection("Data Source=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<AuthenticationDbContext>()
			.UseSqlite(connection, sqlite => sqlite.MigrationsHistoryTable(AuthenticationDbContext.MigrationsHistoryTableName))
			.Options;

		await using var dbContext = new AuthenticationDbContext(options);
		await dbContext.Database.EnsureCreatedAsync();

		return new AuthenticationSqliteTestHarness(connection, new TestDbContextFactory(options));
	}

	public async Task SeedAsync(params LocalUser[] users)
	{
		await using var dbContext = await DbContextFactory.CreateDbContextAsync();
		dbContext.LocalUsers.AddRange(users);
		await dbContext.SaveChangesAsync();
	}

	public async Task<LocalUser?> GetUserAsync(int id)
	{
		await using var dbContext = await DbContextFactory.CreateDbContextAsync();
		return await dbContext.LocalUsers
			.AsNoTracking()
			.SingleOrDefaultAsync(user => user.Id == id);
	}

	public async ValueTask DisposeAsync()
	{
		await connection.DisposeAsync();
	}

	private sealed class TestDbContextFactory(DbContextOptions<AuthenticationDbContext> options) : IDbContextFactory<AuthenticationDbContext>
	{
		public AuthenticationDbContext CreateDbContext()
		{
			return new AuthenticationDbContext(options);
		}

		public Task<AuthenticationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new AuthenticationDbContext(options));
		}
	}
}