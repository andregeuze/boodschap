using Boodschap.Features.Updates.Application.Contracts;
using Microsoft.Extensions.Options;

namespace Boodschap.Features.Updates.Infrastructure;

public sealed class UpdateCheckBackgroundService(
	IUpdateCheckService updateCheckService,
	IOptions<UpdateFeatureOptions> options,
	ILogger<UpdateCheckBackgroundService> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await CheckForUpdatesAsync(stoppingToken);

		var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.CacheDurationMinutes));
		using var timer = new PeriodicTimer(interval);

		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			await CheckForUpdatesAsync(stoppingToken);
		}
	}

	private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
	{
		try
		{
			await updateCheckService.CheckAsync(cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "The background update check failed unexpectedly.");
		}
	}
}