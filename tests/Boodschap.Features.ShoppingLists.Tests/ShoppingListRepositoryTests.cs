using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Features.ShoppingLists.Infrastructure.Persistence;
using Boodschap.Features.ShoppingLists.Tests.Testing;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class ShoppingListRepositoryTests
{
	[Fact]
	public async Task GetListsAsync_ReturnsMostRecentlyUpdatedListFirst()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Older groceries",
				SortOrder = 0,
				UpdatedAt = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc)
			},
			new ShoppingList
			{
				Name = "Recent groceries",
				SortOrder = 1,
				UpdatedAt = new DateTime(2026, 8, 25, 8, 0, 0, DateTimeKind.Utc)
			});

		var repository = new ShoppingListRepository(harness.DbContextFactory);

		var lists = await repository.GetListsAsync();

		Assert.Equal(["Recent groceries", "Older groceries"], lists.Select(list => list.Name).ToArray());
	}

	[Fact]
	public async Task AddItemAsync_InsertsBeforePurchasedItems()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Weekly groceries",
				SortOrder = 0,
				UpdatedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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
		Assert.True(result.Value.UpdatedAt > new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
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

	[Fact]
	public async Task UpdateListDetailsAsync_UpdatesPersistedTrimmedDetails()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Weekly groceries",
				Description = "Fresh groceries",
				Archived = false,
				SortOrder = 0,
				Items = []
			});

		var repository = new ShoppingListRepository(harness.DbContextFactory);
		var list = (await repository.GetListsAsync()).Single();

		var result = await repository.UpdateListDetailsAsync(list.Id, "  Dinner prep  ", "  Weekend meals  ");

		Assert.True(result.Changed);
		Assert.NotNull(result.Value);
		Assert.Equal("Dinner prep", result.Value.Name);
		Assert.Equal("Weekend meals", result.Value.Description);

		var persisted = await harness.GetListAsync(list.Id);
		Assert.NotNull(persisted);
		Assert.Equal("Dinner prep", persisted.Name);
		Assert.Equal("Weekend meals", persisted.Description);
	}

	[Fact]
	public async Task UpdateListDetailsAsync_UpdatesDescriptionWhenNameIsUnchanged()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Weekly groceries",
				Description = "Fresh groceries",
				Archived = false,
				SortOrder = 0,
				Items = []
			});

		var repository = new ShoppingListRepository(harness.DbContextFactory);
		var list = (await repository.GetListsAsync()).Single();

		var result = await repository.UpdateListDetailsAsync(list.Id, "Weekly groceries", "  Pantry staples  ");

		Assert.True(result.Changed);
		Assert.Equal("Pantry staples", result.Value?.Description);

		var persisted = await harness.GetListAsync(list.Id);
		Assert.Equal("Weekly groceries", persisted?.Name);
		Assert.Equal("Pantry staples", persisted?.Description);
	}

	[Fact]
	public async Task RenameItemAsync_UpdatesPersistedTrimmedName()
	{
		await using var harness = await ShoppingListsSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(
			new ShoppingList
			{
				Name = "Weekly groceries",
				Archived = false,
				SortOrder = 0,
				Items =
				[
					new() { Name = "Milk", SortOrder = 0 },
					new() { Name = "Eggs", SortOrder = 1 }
				]
			});

		var repository = new ShoppingListRepository(harness.DbContextFactory);
		var list = (await repository.GetListsAsync()).Single();
		var item = list.Items.Single(entry => entry.Name == "Milk");

		var result = await repository.RenameItemAsync(list.Id, item.Id, "  Oat milk  ");

		Assert.True(result.Changed);
		Assert.NotNull(result.Value);
		Assert.Equal(["Oat milk", "Eggs"], result.Value.Items.Select(entry => entry.Name).ToArray());

		var persisted = await harness.GetListAsync(list.Id);
		Assert.NotNull(persisted);
		Assert.Equal("Oat milk", persisted.Items.Single(entry => entry.Id == item.Id).Name);
	}
}