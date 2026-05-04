using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Features.ShoppingLists.Infrastructure.Persistence;
using Boodschap.Features.ShoppingLists.Tests.Testing;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class ShoppingListRepositoryTests
{
	[Fact]
	public async Task AddItemAsync_InsertsBeforePurchasedItems()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Weekly groceries",
				SortOrder = 0,
				Items =
				[
					new() { Name = "Milk", SortOrder = 0 },
					new() { Name = "Eggs", IsDone = true, SortOrder = 1 }
				]
			});

		var repository = new ShoppingListRepository(harness.DbContextFactory);
		var list = (await repository.GetListsAsync()).Single();

		var result = await repository.AddItemAsync(list.Id, "Bread");

		Assert.True(result.Changed);
		Assert.NotNull(result.Value);
		Assert.Equal(["Milk", "Bread", "Eggs"], result.Value.Items.Select(item => item.Name).ToArray());
	}

	[Fact]
	public async Task ReorderItemAsync_RewritesPersistedSortOrder()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Weekly groceries",
				SortOrder = 0,
				Items =
				[
					new() { Name = "Milk", SortOrder = 0 },
					new() { Name = "Bread", SortOrder = 1 },
					new() { Name = "Eggs", SortOrder = 2 }
				]
			});

		var repository = new ShoppingListRepository(harness.DbContextFactory);
		var list = (await repository.GetListsAsync()).Single();
		var milk = list.Items.Single(item => item.Name == "Milk");
		var eggs = list.Items.Single(item => item.Name == "Eggs");

		var result = await repository.ReorderItemAsync(list.Id, eggs.Id, milk.Id);

		Assert.True(result.Changed);
		Assert.NotNull(result.Value);
		Assert.Equal(["Eggs", "Milk", "Bread"], result.Value.Items.Select(item => item.Name).ToArray());

		var persisted = await harness.GetListAsync(list.Id);
		Assert.NotNull(persisted);
		Assert.Equal(["Eggs", "Milk", "Bread"], persisted.Items.OrderBy(item => item.SortOrder).Select(item => item.Name).ToArray());
		Assert.Equal([0, 1, 2], persisted.Items.OrderBy(item => item.SortOrder).Select(item => item.SortOrder).ToArray());
	}

	[Fact]
	public async Task RemoveArchivedListAsync_RemovesArchivedListAndItems()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Old groceries",
				Archived = true,
				SortOrder = 0,
				Items =
				[
					new() { Name = "Tea", SortOrder = 0 },
					new() { Name = "Sugar", SortOrder = 1 }
				]
			});

		var repository = new ShoppingListRepository(harness.DbContextFactory);
		var list = (await repository.GetListsAsync()).Single();

		var result = await repository.RemoveArchivedListAsync(list.Id);

		Assert.True(result.Changed);
		Assert.Null(result.Value);
		Assert.Null(await harness.GetListAsync(list.Id));
	}

	[Fact]
	public async Task RemoveArchivedListAsync_DoesNotRemoveActiveList()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Current groceries",
				Archived = false,
				SortOrder = 0,
				Items = []
			});

		var repository = new ShoppingListRepository(harness.DbContextFactory);
		var list = (await repository.GetListsAsync()).Single();

		var result = await repository.RemoveArchivedListAsync(list.Id);

		Assert.False(result.Changed);
		Assert.NotNull(result.Value);
		Assert.NotNull(await harness.GetListAsync(list.Id));
	}
}