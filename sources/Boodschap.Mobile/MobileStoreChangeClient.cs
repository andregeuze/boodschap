using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Shared.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boodschap.Mobile;

public sealed class MobileStoreChangeClient : IAsyncDisposable
{
	private readonly IRemoteAuthenticationClient authenticationClient;
	private readonly MobileSessionState sessionState;
	private readonly StoreChangeNotifier notifier;
	private readonly BackendOptions backendOptions;
	private HubConnection? connection;

	public MobileStoreChangeClient(
		IRemoteAuthenticationClient authenticationClient,
		MobileSessionState sessionState,
		StoreChangeNotifier notifier,
		BackendOptions backendOptions)
	{
		this.authenticationClient = authenticationClient;
		this.sessionState = sessionState;
		this.notifier = notifier;
		this.backendOptions = backendOptions;
		sessionState.Changed += HandleSessionChangedAsync;
	}

	public async Task StartAsync()
	{
		await sessionState.InitializeAsync();
		await SynchronizeConnectionAsync(sessionState.CurrentUser);
	}

	public async ValueTask DisposeAsync()
	{
		sessionState.Changed -= HandleSessionChangedAsync;
		if (connection is not null)
		{
			await connection.DisposeAsync();
		}
	}

	private Task HandleSessionChangedAsync(LocalUser? user)
	{
		return SynchronizeConnectionAsync(user);
	}

	private async Task SynchronizeConnectionAsync(LocalUser? user)
	{
		if (user is null)
		{
			if (connection?.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
			{
				try
				{
					await connection.StopAsync();
				}
				catch
				{
				}
			}
			return;
		}

		connection ??= CreateConnection();
		if (connection.State == HubConnectionState.Disconnected)
		{
			try
			{
				await connection.StartAsync();
			}
			catch
			{
				// The native app still works without live refresh; retries happen on the next auth or data change.
			}
		}
	}

	private HubConnection CreateConnection()
	{
		var hubUri = new Uri(backendOptions.GetValidatedBaseUri(), StoreChangeRealtimeDefaults.HubRoute);
		var hubConnection = new HubConnectionBuilder()
			.WithUrl(hubUri, options => options.AccessTokenProvider = () => authenticationClient.GetAccessTokenAsync())
			.WithAutomaticReconnect()
			.Build();

		hubConnection.On<StoreChangedMessage>("StoreChanged", message =>
			notifier.NotifyChangedAsync(new StoreChange(message.ListId)));
		return hubConnection;
	}
}