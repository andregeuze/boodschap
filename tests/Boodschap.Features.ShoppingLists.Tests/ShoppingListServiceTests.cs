using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Shared.Realtime;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class ShoppingListServiceTests
{
	[Fact]
	public async Task CreateListAsync_NotifiesCreatedListId()
	{
		var createdList = new ShoppingList
		{
			Id = 42,
			Name = "Weekend groceries",
			Items = []
		};
		var repository = new FakeShoppingListRepository
		{
			CreatedList = createdList
		};
		var notifier = new StoreChangeNotifier();
		var observedChanges = new List<StoreChange>();
		notifier.Changed += change =>
		{
			observedChanges.Add(change);
			return Task.CompletedTask;
		};

		var service = new ShoppingListService(repository, notifier);

		var result = await service.CreateListAsync("Weekend groceries", "Snacks and breakfast for the weekend.");

		Assert.Same(createdList, result);
		Assert.Equal("Weekend groceries", repository.LastCreatedName);
		Assert.Equal("Snacks and breakfast for the weekend.", repository.LastCreatedDescription);
		Assert.Single(observedChanges);
		Assert.Equal(42, observedChanges[0].ListId);
	}

	[Fact]
	public async Task ArchiveListAsync_DoesNotNotifyWhenRepositoryReportsNoChange()
	{
		var repository = new FakeShoppingListRepository
		{
			ArchiveResult = new MutationResult<ShoppingList>(
				new ShoppingList
				{
					Id = 7,
					Name = "Weekly groceries",
					Items = []
				},
				Changed: false)
		};
		var notifier = new StoreChangeNotifier();
		var notificationCount = 0;
		notifier.Changed += _ =>
		{
			notificationCount++;
			return Task.CompletedTask;
		};

		var service = new ShoppingListService(repository, notifier);

		var result = await service.ArchiveListAsync(7);

		Assert.NotNull(result);
		Assert.Equal(0, notificationCount);
	}

	[Fact]
	public async Task RemoveArchivedListAsync_NotifiesRemovedListId()
	{
		var repository = new FakeShoppingListRepository
		{
			RemoveArchivedListResult = new MutationResult<ShoppingList>(null, Changed: true)
		};
		var notifier = new StoreChangeNotifier();
		var observedChanges = new List<StoreChange>();
		notifier.Changed += change =>
		{
			observedChanges.Add(change);
			return Task.CompletedTask;
		};

		var service = new ShoppingListService(repository, notifier);

		var result = await service.RemoveArchivedListAsync(11);

		Assert.True(result);
		Assert.Single(observedChanges);
		Assert.Equal(11, observedChanges[0].ListId);
	}

	[Fact]
	public async Task UpdateListDetailsAsync_NotifiesUpdatedListId()
	{
		var repository = new FakeShoppingListRepository
		{
			UpdateListDetailsResult = new MutationResult<ShoppingList>(
				new ShoppingList
				{
					Id = 13,
					Name = "Party supplies",
					Description = "Dinner and decorations",
					Items = []
				},
				Changed: true)
		};
		var notifier = new StoreChangeNotifier();
		var observedChanges = new List<StoreChange>();
		notifier.Changed += change =>
		{
			observedChanges.Add(change);
			return Task.CompletedTask;
		};

		var service = new ShoppingListService(repository, notifier);

		var result = await service.UpdateListDetailsAsync(13, "Party supplies", "Dinner and decorations");

		Assert.NotNull(result);
		Assert.Equal("Party supplies", repository.LastUpdatedName);
		Assert.Equal("Dinner and decorations", repository.LastUpdatedDescription);
		Assert.Single(observedChanges);
		Assert.Equal(13, observedChanges[0].ListId);
	}

	[Fact]
	public async Task RenameItemAsync_NotifiesChangedListId()
	{
		var repository = new FakeShoppingListRepository
		{
			RenameItemResult = new MutationResult<ShoppingList>(
				new ShoppingList
				{
					Id = 13,
					Name = "Party supplies",
					Items =
					[
						new() { Id = 8, Name = "Sparkling water" }
					]
				},
				Changed: true)
		};
		var notifier = new StoreChangeNotifier();
		var observedChanges = new List<StoreChange>();
		notifier.Changed += change =>
		{
			observedChanges.Add(change);
			return Task.CompletedTask;
		};

		var service = new ShoppingListService(repository, notifier);

		var result = await service.RenameItemAsync(13, 8, "Sparkling water");

		Assert.NotNull(result);
		Assert.Equal(8, repository.LastRenamedItemId);
		Assert.Equal("Sparkling water", repository.LastRenamedItemName);
		Assert.Single(observedChanges);
		Assert.Equal(13, observedChanges[0].ListId);
	}

	private sealed class FakeShoppingListRepository : IShoppingListRepository
	{
		public ShoppingList CreatedList { get; set; } = new()
		{
			Id = 1,
			Name = "Created",
			Items = []
		};

		public MutationResult<ShoppingList> ArchiveResult { get; set; }
		public MutationResult<ShoppingList> UpdateListDetailsResult { get; set; }
		public MutationResult<ShoppingList> RenameItemResult { get; set; }
		public MutationResult<ShoppingList> RemoveArchivedListResult { get; set; }

		public string? LastCreatedName { get; private set; }
		public string? LastCreatedDescription { get; private set; }
		public string? LastUpdatedName { get; private set; }
		public string? LastUpdatedDescription { get; private set; }
		public int? LastRenamedItemId { get; private set; }
		public string? LastRenamedItemName { get; private set; }

		public Task<IReadOnlyList<ShoppingList>> GetListsAsync(CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<ShoppingList?> GetListAsync(int id, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<ShoppingList> CreateListAsync(string name, string description, CancellationToken cancellationToken = default)
		{
			LastCreatedName = name;
			LastCreatedDescription = description;
			return Task.FromResult(CreatedList);
		}

		public Task<MutationResult<ShoppingList>> SetListArchivedStateAsync(int listId, bool archived, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(ArchiveResult);
		}

		public Task<MutationResult<ShoppingList>> UpdateListDetailsAsync(int listId, string name, string description, CancellationToken cancellationToken = default)
		{
			LastUpdatedName = name;
			LastUpdatedDescription = description;
			return Task.FromResult(UpdateListDetailsResult);
		}

		public Task<MutationResult<ShoppingList>> RemoveArchivedListAsync(int listId, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(RemoveArchivedListResult);
		}

		public Task<MutationResult<ShoppingList>> AddItemAsync(int listId, string itemName, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<MutationResult<ShoppingList>> RenameItemAsync(int listId, int itemId, string name, CancellationToken cancellationToken = default)
		{
			LastRenamedItemId = itemId;
			LastRenamedItemName = name;
			return Task.FromResult(RenameItemResult);
		}

		public Task<MutationResult<ShoppingList>> ToggleDoneAsync(int listId, int itemId, bool isDone, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<MutationResult<ShoppingList>> RemoveItemAsync(int listId, int itemId, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<MutationResult<ShoppingList>> ReorderItemAsync(int listId, int itemId, int targetItemId, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}
	}
}