using Boodschap.Features.Authentication.Application;
using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Shared.Realtime;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boodschap.Mobile;

public sealed class MobileStoreChangeClient : IAsyncDisposable
{
	private readonly IRemoteAuthenticationClient authenticationClient;
	private readonly MobileAuthenticationStateProvider authenticationStateProvider;
	private readonly StoreChangeNotifier notifier;
	private readonly BackendOptions backendOptions;
	private HubConnection? connection;

	public MobileStoreChangeClient(
		IRemoteAuthenticationClient authenticationClient,
		MobileAuthenticationStateProvider authenticationStateProvider,
		StoreChangeNotifier notifier,
		BackendOptions backendOptions)
	{
		this.authenticationClient = authenticationClient;
		this.authenticationStateProvider = authenticationStateProvider;
		this.notifier = notifier;
		this.backendOptions = backendOptions;
		authenticationStateProvider.AuthenticationStateChanged += HandleAuthenticationStateChanged;
	}

	public async Task StartAsync()
	{
		var state = await authenticationStateProvider.GetAuthenticationStateAsync();
		await SynchronizeConnectionAsync(state);
	}

	public async ValueTask DisposeAsync()
	{
		authenticationStateProvider.AuthenticationStateChanged -= HandleAuthenticationStateChanged;
		if (connection is not null)
		{
			await connection.DisposeAsync();
		}
	}

	private void HandleAuthenticationStateChanged(Task<AuthenticationState> stateTask)
	{
		_ = SynchronizeConnectionAsync(stateTask);
	}

	private async Task SynchronizeConnectionAsync(Task<AuthenticationState> stateTask)
	{
		await SynchronizeConnectionAsync(await stateTask);
	}

	private async Task SynchronizeConnectionAsync(AuthenticationState state)
	{
		if (state.User.Identity?.IsAuthenticated != true)
		{
			if (connection?.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
			{
				await connection.StopAsync();
			}
			return;
		}

		connection ??= CreateConnection();
		if (connection.State == HubConnectionState.Disconnected)
		{
			await connection.StartAsync();
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