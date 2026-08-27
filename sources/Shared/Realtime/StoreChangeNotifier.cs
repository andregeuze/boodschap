namespace Boodschap.Shared.Realtime;

public sealed class StoreChangeNotifier
{
	public event Func<StoreChange, Task>? Changed;

	public Task NotifyChangedAsync(StoreChange change)
	{
		var handlers = Changed;
		if (handlers is null)
		{
			return Task.CompletedTask;
		}

		return Task.WhenAll(handlers.GetInvocationList()
			.Cast<Func<StoreChange, Task>>()
			.Select(handler => handler(change)));
	}
}

public readonly record struct StoreChange(int? ListId);