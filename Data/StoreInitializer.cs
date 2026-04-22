using Microsoft.EntityFrameworkCore;

namespace Boodschap.Data;

public static class StoreInitializer
{
	public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
	{
		await using var scope = services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BoodschapDbContext>>();
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		await dbContext.Database.EnsureCreatedAsync(cancellationToken);
		if (await dbContext.ShoppingLists.AnyAsync(cancellationToken))
		{
			return;
		}

		dbContext.ShoppingLists.AddRange(
			new ShoppingList
			{
				Name = "Weekly groceries",
				Description = "Fresh produce, dairy, and pantry basics.",
				Archived = false,
				SortOrder = 0,
				Items =
				[
					new() { Name = "Milk", SortOrder = 0 },
					new() { Name = "Eggs", SortOrder = 1 },
					new() { Name = "Bread", SortOrder = 2 },
					new() { Name = "Tomatoes", SortOrder = 3 },
					new() { Name = "Cheese", SortOrder = 4 },
					new() { Name = "Coffee", SortOrder = 5 }
				]
			},
			new ShoppingList
			{
				Name = "Dinner party",
				Description = "Everything for Friday night's cooking plan.",
				Archived = false,
				SortOrder = 1,
				Items =
				[
					new() { Name = "Pasta", SortOrder = 0 },
					new() { Name = "Basil", SortOrder = 1 },
					new() { Name = "Parmesan", SortOrder = 2 },
					new() { Name = "Olive oil", SortOrder = 3 }
				]
			},
			new ShoppingList
			{
				Name = "Camping weekend",
				Description = "Packed and completed for the last trip.",
				Archived = true,
				SortOrder = 2,
				Items =
				[
					new() { Name = "Trail mix", IsDone = true, SortOrder = 0 },
					new() { Name = "Water bottles", IsDone = true, SortOrder = 1 },
					new() { Name = "Instant noodles", IsDone = true, SortOrder = 2 }
				]
			});

		await dbContext.SaveChangesAsync(cancellationToken);
	}
}