using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Presentation;
using Boodschap.Shared.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class StoreChangeBroadcastServiceTests
{
	[Fact]
	public async Task StartedService_BroadcastsNotifierChangesOnce()
	{
		var notifier = new StoreChangeNotifier();
		var proxy = new RecordingClientProxy();
		var service = new StoreChangeBroadcastService(notifier, new RecordingHubContext(proxy));
		await service.StartAsync(CancellationToken.None);

		await notifier.NotifyChangedAsync(new StoreChange(17));
		await service.StopAsync(CancellationToken.None);
		await notifier.NotifyChangedAsync(new StoreChange(18));

		var message = Assert.Single(proxy.Messages);
		Assert.Equal("StoreChanged", message.Method);
		Assert.Equal(17, Assert.IsType<StoreChangedMessage>(Assert.Single(message.Arguments)).ListId);
	}

	private sealed class RecordingClientProxy : IClientProxy
	{
		public List<(string Method, object?[] Arguments)> Messages { get; } = [];

		public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
		{
			Messages.Add((method, args));
			return Task.CompletedTask;
		}
	}

	private sealed class RecordingHubContext(RecordingClientProxy proxy) : IHubContext<ShoppingListUpdatesHub>
	{
		public IHubClients Clients { get; } = new RecordingHubClients(proxy);
		public IGroupManager Groups { get; } = new NoOpGroupManager();
	}

	private sealed class RecordingHubClients(IClientProxy proxy) : IHubClients
	{
		public IClientProxy All => proxy;
		public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => proxy;
		public IClientProxy Client(string connectionId) => proxy;
		public IClientProxy Clients(IReadOnlyList<string> connectionIds) => proxy;
		public IClientProxy Group(string groupName) => proxy;
		public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => proxy;
		public IClientProxy Groups(IReadOnlyList<string> groupNames) => proxy;
		public IClientProxy User(string userId) => proxy;
		public IClientProxy Users(IReadOnlyList<string> userIds) => proxy;
	}

	private sealed class NoOpGroupManager : IGroupManager
	{
		public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}