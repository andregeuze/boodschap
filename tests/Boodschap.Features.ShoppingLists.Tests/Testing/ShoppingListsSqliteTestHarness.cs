using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Features.ShoppingLists.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.ShoppingLists.Tests.Testing;

public sealed class ShoppingListsSqliteTestHarness : IAsyncDisposable
{
	private readonly SqliteConnection connection;

	private ShoppingListsSqliteTestHarness(
		SqliteConnection connection,
		IDbContextFactory<BoodschapDbContext> dbContextFactory)
	{
		this.connection = connection;
		DbContextFactory = dbContextFactory;
	}

	public IDbContextFactory<BoodschapDbContext> DbContextFactory { get; }

	public static async Task<ShoppingListsSqliteTestHarness> CreateAsync()
	{
		var connection = new SqliteConnection("Data Source=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<BoodschapDbContext>()
			.UseSqlite(connection)
			.Options;

		await using var dbContext = new BoodschapDbContext(options);
		await dbContext.Database.EnsureCreatedAsync();

		return new ShoppingListsSqliteTestHarness(connection, new TestDbContextFactory(options));
	}

	public async Task SeedAsync(params ShoppingList[] shoppingLists)
	{
		await using var dbContext = await DbContextFactory.CreateDbContextAsync();
		dbContext.ShoppingLists.AddRange(shoppingLists);
		await dbContext.SaveChangesAsync();
	}

	public async Task<ShoppingList?> GetListAsync(int id)
	{
		await using var dbContext = await DbContextFactory.CreateDbContextAsync();
		return await dbContext.ShoppingLists
			.Include(list => list.Items)
			.SingleOrDefaultAsync(list => list.Id == id);
	}

	public async ValueTask DisposeAsync()
	{
		await connection.DisposeAsync();
	}

	private sealed class TestDbContextFactory(DbContextOptions<BoodschapDbContext> options) : IDbContextFactory<BoodschapDbContext>
	{
		public BoodschapDbContext CreateDbContext()
		{
			return new BoodschapDbContext(options);
		}

		public Task<BoodschapDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new BoodschapDbContext(options));
		}
	}
}