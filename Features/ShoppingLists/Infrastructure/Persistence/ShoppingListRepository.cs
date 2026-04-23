using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.ShoppingLists.Infrastructure.Persistence;

public sealed class ShoppingListRepository(IDbContextFactory<BoodschapDbContext> dbContextFactory) : IShoppingListRepository
{
	public async Task<IReadOnlyList<ShoppingList>> GetListsAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var shoppingLists = await dbContext.ShoppingLists
			.AsNoTracking()
			.Include(list => list.Items)
			.OrderBy(list => list.SortOrder)
			.ToListAsync(cancellationToken);

		return [.. shoppingLists.Select(MapList)];
	}

	public async Task<ShoppingList?> GetListAsync(int id, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var shoppingList = await dbContext.ShoppingLists
			.AsNoTracking()
			.Include(list => list.Items)
			.SingleOrDefaultAsync(list => list.Id == id, cancellationToken);

		return shoppingList is null ? null : MapList(shoppingList);
	}

	public async Task<ShoppingList> CreateListAsync(string name, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var nextSortOrder = (await dbContext.ShoppingLists.MinAsync(list => (int?)list.SortOrder, cancellationToken) ?? 0) - 1;

		var shoppingList = new ShoppingList
		{
			Name = name.Trim(),
			Description = "A fresh list ready for new items.",
			Archived = false,
			SortOrder = nextSortOrder,
			Items = []
		};

		dbContext.ShoppingLists.Add(shoppingList);
		await dbContext.SaveChangesAsync(cancellationToken);

		return await GetListRequiredAsync(shoppingList.Id, cancellationToken);
	}

	public async Task<MutationResult<ShoppingList>> SetListArchivedStateAsync(int listId, bool archived, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var shoppingList = await dbContext.ShoppingLists
			.SingleOrDefaultAsync(list => list.Id == listId, cancellationToken);
		if (shoppingList is null)
		{
			return new(null, false);
		}

		if (shoppingList.Archived == archived)
		{
			return new(await GetListAsync(listId, cancellationToken), false);
		}

		shoppingList.Archived = archived;
		await dbContext.SaveChangesAsync(cancellationToken);

		return new(await GetListAsync(listId, cancellationToken), true);
	}

	public async Task<MutationResult<ShoppingList>> AddItemAsync(int listId, string itemName, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(itemName))
		{
			return new(await GetListAsync(listId, cancellationToken), false);
		}

		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var items = await dbContext.ShoppingListItems
			.Where(item => item.ShoppingListId == listId)
			.OrderBy(item => item.SortOrder)
			.ToListAsync(cancellationToken);

		var shoppingListExists = items.Count != 0
			|| await dbContext.ShoppingLists.AnyAsync(list => list.Id == listId, cancellationToken);
		if (!shoppingListExists)
		{
			return new(null, false);
		}

		var insertIndex = items.FindIndex(item => item.IsDone);
		if (insertIndex < 0)
		{
			insertIndex = items.Count;
		}

		var newItem = new ShoppingListItem
		{
			ShoppingListId = listId,
			Name = itemName.Trim(),
			SortOrder = insertIndex
		};

		items.Insert(insertIndex, newItem);

		for (var index = 0; index < items.Count; index++)
		{
			items[index].SortOrder = index;
		}

		dbContext.ShoppingListItems.Add(newItem);
		await dbContext.SaveChangesAsync(cancellationToken);

		return new(await GetListAsync(listId, cancellationToken), true);
	}

	public async Task<MutationResult<ShoppingList>> ToggleDoneAsync(int listId, int itemId, bool isDone, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var item = await dbContext.ShoppingListItems
			.SingleOrDefaultAsync(entry => entry.ShoppingListId == listId && entry.Id == itemId, cancellationToken);
		if (item is null)
		{
			return new(null, false);
		}

		if (item.IsDone == isDone)
		{
			return new(await GetListAsync(listId, cancellationToken), false);
		}

		item.IsDone = isDone;

		if (isDone)
		{
			item.SortOrder = (await dbContext.ShoppingListItems
				.Where(entry => entry.ShoppingListId == listId)
				.MaxAsync(entry => (int?)entry.SortOrder, cancellationToken) ?? -1) + 1;
		}

		await dbContext.SaveChangesAsync(cancellationToken);

		return new(await GetListAsync(listId, cancellationToken), true);
	}

	public async Task<MutationResult<ShoppingList>> RemoveItemAsync(int listId, int itemId, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var item = await dbContext.ShoppingListItems
			.SingleOrDefaultAsync(entry => entry.ShoppingListId == listId && entry.Id == itemId, cancellationToken);
		if (item is null)
		{
			return new(null, false);
		}

		dbContext.ShoppingListItems.Remove(item);
		await dbContext.SaveChangesAsync(cancellationToken);

		return new(await GetListAsync(listId, cancellationToken), true);
	}

	public async Task<MutationResult<ShoppingList>> ReorderItemAsync(int listId, int itemId, int targetItemId, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var items = await dbContext.ShoppingListItems
			.Where(item => item.ShoppingListId == listId)
			.OrderBy(item => item.SortOrder)
			.ToListAsync(cancellationToken);

		var draggedItem = items.FirstOrDefault(item => item.Id == itemId);
		var targetItem = items.FirstOrDefault(item => item.Id == targetItemId);
		if (draggedItem is null || targetItem is null || draggedItem.Id == targetItem.Id)
		{
			return new(await GetListAsync(listId, cancellationToken), false);
		}

		var from = items.IndexOf(draggedItem);
		var to = items.IndexOf(targetItem);
		items.RemoveAt(from);
		items.Insert(to, draggedItem);

		for (var index = 0; index < items.Count; index++)
		{
			items[index].SortOrder = index;
		}

		await dbContext.SaveChangesAsync(cancellationToken);

		return new(await GetListAsync(listId, cancellationToken), true);
	}

	private async Task<ShoppingList> GetListRequiredAsync(int id, CancellationToken cancellationToken)
	{
		var shoppingList = await GetListAsync(id, cancellationToken);
		return shoppingList ?? throw new InvalidOperationException($"Shopping list {id} was not found after it was created.");
	}

	private static ShoppingList MapList(ShoppingList shoppingList)
	{
		return new ShoppingList
		{
			Id = shoppingList.Id,
			Name = shoppingList.Name,
			Description = shoppingList.Description,
			Archived = shoppingList.Archived,
			SortOrder = shoppingList.SortOrder,
			Items = shoppingList.Items
				.OrderBy(item => item.SortOrder)
				.Select(item => new ShoppingListItem
				{
					Id = item.Id,
					ShoppingListId = item.ShoppingListId,
					Name = item.Name,
					IsDone = item.IsDone,
					SortOrder = item.SortOrder
				})
				.ToList()
		};
	}
}