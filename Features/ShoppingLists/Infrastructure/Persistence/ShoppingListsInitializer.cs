using Boodschap.Features.ShoppingLists.Domain;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.ShoppingLists.Infrastructure.Persistence;

public static class ShoppingListsInitializer
{
	public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
	{
		await using var scope = services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BoodschapDbContext>>();
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		await dbContext.Database.MigrateAsync(cancellationToken);
		if (await dbContext.ShoppingLists.AnyAsync(cancellationToken))
		{
			return;
		}

		dbContext.ShoppingLists.AddRange(
			new ShoppingList
			{
				Name = "Weekboodschappen",
				Description = "Verse producten, zuivel en voorraadkast basics.",
				Archived = false,
				SortOrder = 0,
				Items =
				[
					new() { Name = "Melk", SortOrder = 0 },
					new() { Name = "Eieren", SortOrder = 1 },
					new() { Name = "Brood", SortOrder = 2 },
					new() { Name = "Tomaten", SortOrder = 3 },
					new() { Name = "Kaas", SortOrder = 4 },
					new() { Name = "Koffie", SortOrder = 5 }
				]
			},
			new ShoppingList
			{
				Name = "Etentje",
				Description = "Alles voor het kookplan van vrijdagavond.",
				Archived = false,
				SortOrder = 1,
				Items =
				[
					new() { Name = "Pasta", SortOrder = 0 },
					new() { Name = "Basilicum", SortOrder = 1 },
					new() { Name = "Parmezaan", SortOrder = 2 },
					new() { Name = "Olijfolie", SortOrder = 3 }
				]
			},
			new ShoppingList
			{
				Name = "Kampeerweekend",
				Description = "Ingepakt en afgerond voor de vorige trip.",
				Archived = true,
				SortOrder = 2,
				Items =
				[
					new() { Name = "Notenmix", IsDone = true, SortOrder = 0 },
					new() { Name = "Waterflessen", IsDone = true, SortOrder = 1 },
					new() { Name = "Instant noedels", IsDone = true, SortOrder = 2 }
				]
			});

		await dbContext.SaveChangesAsync(cancellationToken);
	}
}