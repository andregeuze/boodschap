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

		var result = await service.CreateListAsync("Weekend groceries");

		Assert.Same(createdList, result);
		Assert.Equal("Weekend groceries", repository.LastCreatedName);
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

	private sealed class FakeShoppingListRepository : IShoppingListRepository
	{
		public ShoppingList CreatedList { get; set; } = new()
		{
			Id = 1,
			Name = "Created",
			Items = []
		};

		public MutationResult<ShoppingList> ArchiveResult { get; set; }

		public string? LastCreatedName { get; private set; }

		public Task<IReadOnlyList<ShoppingList>> GetListsAsync(CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<ShoppingList?> GetListAsync(int id, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}

		public Task<ShoppingList> CreateListAsync(string name, CancellationToken cancellationToken = default)
		{
			LastCreatedName = name;
			return Task.FromResult(CreatedList);
		}

		public Task<MutationResult<ShoppingList>> SetListArchivedStateAsync(int listId, bool archived, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(ArchiveResult);
		}

		public Task<MutationResult<ShoppingList>> AddItemAsync(int listId, string itemName, CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
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