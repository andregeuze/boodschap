using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Shared.Realtime;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Boodschap.Mobile;

public sealed class MobileStoreChangeClient : IAsyncDisposable
{
	private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
	private const string AndroidLogTag = "Boodschap.SignalR";
	private readonly SemaphoreSlim synchronizationLock = new(1, 1);
	private readonly IRemoteAuthenticationClient authenticationClient;
	private readonly MobileSessionState sessionState;
	private readonly StoreChangeNotifier notifier;
	private readonly BackendOptions backendOptions;
	private readonly ILogger<MobileStoreChangeClient> logger;
	private HubConnection? connection;
	private Uri? currentHubBaseUri;
	private Uri? currentHubUri;
	private CancellationTokenSource? reconnectCancellationTokenSource;
	private Task? reconnectTask;
	private LocalUser? currentUser;

	public MobileStoreChangeClient(
		IRemoteAuthenticationClient authenticationClient,
		MobileSessionState sessionState,
		StoreChangeNotifier notifier,
		BackendOptions backendOptions,
		ILogger<MobileStoreChangeClient> logger)
	{
		this.authenticationClient = authenticationClient;
		this.sessionState = sessionState;
		this.notifier = notifier;
		this.backendOptions = backendOptions;
		this.logger = logger;
		sessionState.Changed += HandleSessionChangedAsync;
		LogInfo("Mobile store-change client created.");
	}

	public async Task StartAsync()
	{
		LogInfo("Mobile store-change client starting.");
		await sessionState.InitializeAsync();
		LogInfo($"Mobile store-change client start sees {(sessionState.CurrentUser is null ? "no signed-in user" : $"user '{sessionState.CurrentUser.Username}'")}.");
		await SynchronizeConnectionAsync(sessionState.CurrentUser);
	}

	public async ValueTask DisposeAsync()
	{
		sessionState.Changed -= HandleSessionChangedAsync;
		StopReconnectLoop();
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
		await synchronizationLock.WaitAsync();
		try
		{
			currentUser = user;
			if (user is null)
			{
				StopReconnectLoop();
				if (connection?.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
				{
					try
					{
						await connection.StopAsync();
					}
					catch (Exception exception)
					{
						logger.LogDebug(exception, "Stopping the mobile store-change hub connection failed.");
					}
				}

				return;
			}

			connection ??= CreateConnection();
			if (connection.State == HubConnectionState.Disconnected)
			{
				if (await TryStartConnectionAsync(CancellationToken.None))
				{
					return;
				}

				EnsureReconnectLoop();
			}
		}
		finally
		{
			synchronizationLock.Release();
		}
	}

	private HubConnection CreateConnection(Uri hubBaseUri)
	{
		currentHubBaseUri = hubBaseUri;
		var hubUri = new Uri(hubBaseUri, StoreChangeRealtimeDefaults.HubRoute);
		currentHubUri = hubUri;
		var useLongPollingOnly = hubUri.Scheme == Uri.UriSchemeHttp && hubUri.IsLoopback;
		var hubConnection = new HubConnectionBuilder()
			.WithUrl(hubUri, options =>
			{
				options.AccessTokenProvider = () => authenticationClient.GetAccessTokenAsync();
				options.HttpMessageHandlerFactory = innerHandler => new AuthenticatedHttpMessageHandler(authenticationClient, sessionState)
				{
					InnerHandler = innerHandler
				};
				if (useLongPollingOnly)
				{
					options.Transports = HttpTransportType.LongPolling;
				}
			})
			.ConfigureLogging(logging =>
			{
				logging.AddDebug();
				logging.SetMinimumLevel(LogLevel.Debug);
			})
			.WithAutomaticReconnect()
			.Build();

		LogInfo($"Configured mobile store-change hub at {hubUri}.");
		if (useLongPollingOnly)
		{
			LogInfo("Using SignalR LongPolling for the local Android debug connection.");
		}

		hubConnection.On<StoreChangedMessage>("StoreChanged", message =>
		{
			LogInfo($"Received StoreChanged for list {message.ListId?.ToString() ?? "<all>"}.");
			return notifier.NotifyChangedAsync(new StoreChange(message.ListId));
		});
		hubConnection.Reconnected += HandleConnectionReconnectedAsync;
		hubConnection.Closed += HandleConnectionClosedAsync;
		return hubConnection;
	}

	private HubConnection CreateConnection()
	{
		return CreateConnection(backendOptions.GetValidatedStoreChangesBaseUri());
	}

	private async Task<bool> TryStartConnectionAsync(CancellationToken cancellationToken)
	{
		if (connection is null || currentUser is null)
		{
			return false;
		}

		if (connection.State != HubConnectionState.Disconnected)
		{
			return true;
		}

		try
		{
			LogInfo($"Connecting mobile store-change hub to {currentHubUri}.");
			await connection.StartAsync(cancellationToken);
			LogInfo("Mobile store-change hub connected.");
			StopReconnectLoop();
			return true;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return false;
		}
		catch (Exception exception)
		{
			if (await TryFallbackToBaseUrlConnectionAsync(cancellationToken, exception))
			{
				return true;
			}

			LogWarning(exception, "Connecting the mobile store-change hub failed; a retry loop will keep trying while the user stays signed in.");
			return false;
		}
	}

	private async Task<bool> TryFallbackToBaseUrlConnectionAsync(CancellationToken cancellationToken, Exception originalException)
	{
		var apiBaseUri = backendOptions.GetValidatedBaseUri();
 
		if (currentHubBaseUri is null || Uri.Compare(currentHubBaseUri, apiBaseUri, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0)
		{
			return false;
		}

		LogWarning(originalException, $"Connecting the hosted mobile store-change hub failed; falling back to the API host hub at {apiBaseUri}.");
		await connection!.DisposeAsync();
		connection = CreateConnection(apiBaseUri);

		try
		{
			LogInfo($"Connecting fallback mobile store-change hub to {currentHubUri}.");
			await connection.StartAsync(cancellationToken);
			LogInfo("Fallback mobile store-change hub connected.");
			StopReconnectLoop();
			return true;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return false;
		}
		catch (Exception fallbackException)
		{
			LogWarning(fallbackException, "Connecting the fallback mobile store-change hub also failed.");
			return false;
		}
	}

	private void EnsureReconnectLoop()
	{
		if (reconnectTask is { IsCompleted: false })
		{
			return;
		}

		reconnectCancellationTokenSource?.Dispose();
		reconnectCancellationTokenSource = new CancellationTokenSource();
		reconnectTask = Task.Run(() => ReconnectLoopAsync(reconnectCancellationTokenSource.Token));
	}

	private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(RetryDelay, cancellationToken);

				await synchronizationLock.WaitAsync(cancellationToken);
				try
				{
					if (currentUser is null || connection is null)
					{
						return;
					}

					if (connection.State == HubConnectionState.Connected)
					{
						return;
					}

					if (connection.State == HubConnectionState.Disconnected && await TryStartConnectionAsync(cancellationToken))
					{
						await notifier.NotifyChangedAsync(new StoreChange(ListId: null));
						return;
					}
				}
				finally
				{
					synchronizationLock.Release();
				}
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				return;
			}
		}
	}

	private Task HandleConnectionReconnectedAsync(string? _)
	{
		LogInfo("Mobile store-change hub reconnected.");
		return notifier.NotifyChangedAsync(new StoreChange(ListId: null));
	}

	private async Task HandleConnectionClosedAsync(Exception? exception)
	{
		if (currentUser is null)
		{
			return;
		}

		if (exception is not null)
		{
			LogWarning(exception, "Mobile store-change hub closed unexpectedly; scheduling reconnect attempts.");
		}
		else
		{
			LogInfo("Mobile store-change hub closed; scheduling reconnect attempts while signed in.");
		}

		await synchronizationLock.WaitAsync();
		try
		{
			if (currentUser is not null)
			{
				EnsureReconnectLoop();
			}
		}
		finally
		{
			synchronizationLock.Release();
		}
	}

	private void StopReconnectLoop()
	{
		var reconnectCancellation = reconnectCancellationTokenSource;

		reconnectCancellationTokenSource = null;
		this.reconnectTask = null;

		if (reconnectCancellation is null)
		{
			return;
		}

		reconnectCancellation.Cancel();
		reconnectCancellation.Dispose();
	}

	private void LogInfo(string message)
	{
		logger.LogInformation(message);
#if ANDROID
		Android.Util.Log.Info(AndroidLogTag, message);
#endif
	}

	private void LogWarning(Exception exception, string message)
	{
		logger.LogWarning(exception, message);
#if ANDROID
		Android.Util.Log.Warn(AndroidLogTag, $"{message}{Environment.NewLine}{exception}");
#endif
	}
}