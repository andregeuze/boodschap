using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Shared.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Boodschap.Features.ShoppingLists.Presentation;

public sealed class StoreChangeBroadcastService(
	StoreChangeNotifier notifier,
	IHubContext<ShoppingListUpdatesHub> hubContext) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		notifier.Changed += BroadcastAsync;
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		notifier.Changed -= BroadcastAsync;
		return Task.CompletedTask;
	}

	private Task BroadcastAsync(StoreChange change)
	{
		return hubContext.Clients.All.SendAsync(
			"StoreChanged",
			new StoreChangedMessage(change.ListId),
			CancellationToken.None);
	}
}