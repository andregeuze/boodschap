using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;

namespace Boodschap.Mobile;

public sealed class MobileSessionState(IRemoteAuthenticationClient authenticationClient) : ILocalAuthenticationSession
{
	private readonly SemaphoreSlim initializationLock = new(1, 1);
	private bool initialized;

	public LocalUser? CurrentUser { get; private set; }

	public event Func<LocalUser?, Task>? Changed;

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (initialized)
		{
			return;
		}

		await initializationLock.WaitAsync(cancellationToken);
		try
		{
			if (initialized)
			{
				return;
			}

			CurrentUser = await authenticationClient.GetCurrentUserAsync(cancellationToken);
			initialized = true;
		}
		finally
		{
			initializationLock.Release();
		}
	}

	public Task SignInAsync(LocalUser user)
	{
		return SetCurrentUserAsync(user);
	}

	public async Task SignOutAsync()
	{
		await authenticationClient.ClearSessionAsync();
		await SetCurrentUserAsync(null);
	}

	public Task SetAnonymousAsync()
	{
		return SetCurrentUserAsync(null);
	}

	private Task SetCurrentUserAsync(LocalUser? user)
	{
		initialized = true;
		CurrentUser = user;
		var handlers = Changed;
		if (handlers is null)
		{
			return Task.CompletedTask;
		}

		return Task.WhenAll(handlers.GetInvocationList()
			.Cast<Func<LocalUser?, Task>>()
			.Select(handler => handler(user)));
	}
}