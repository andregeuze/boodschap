namespace Boodschap.Mobile;

public sealed class AppInitializationService(
	MobileSessionState sessionState,
	MobileStoreChangeClient storeChangeClient)
{
	private readonly SemaphoreSlim initializationLock = new(1, 1);
	private bool initialized;

	public async Task InitializeAsync()
	{
		if (initialized)
		{
			return;
		}

		await initializationLock.WaitAsync();
		try
		{
			if (initialized)
			{
				return;
			}

			await sessionState.InitializeAsync();
			await storeChangeClient.StartAsync();
			initialized = true;
		}
		finally
		{
			initializationLock.Release();
		}
	}
}