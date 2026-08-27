using Boodschap.Features.Updates;
using Boodschap.Features.Updates.Application.Contracts;
using Boodschap.Features.Updates.Domain;
using Boodschap.Features.Updates.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Boodschap.Features.Updates.Tests;

public sealed class UpdateCheckBackgroundServiceTests
{
	[Fact]
	public async Task StartAsync_ChecksForUpdatesImmediately()
	{
		var updateCheckService = new RecordingUpdateCheckService();
		var backgroundService = new UpdateCheckBackgroundService(
			updateCheckService,
			Options.Create(new UpdateFeatureOptions { CacheDurationMinutes = 15 }),
			NullLogger<UpdateCheckBackgroundService>.Instance);

		await backgroundService.StartAsync(CancellationToken.None);
		await updateCheckService.Checked.Task.WaitAsync(TimeSpan.FromSeconds(2));
		await backgroundService.StopAsync(CancellationToken.None);

		Assert.Equal(1, updateCheckService.CheckCount);
	}

	private sealed class RecordingUpdateCheckService : IUpdateCheckService
	{
		public TaskCompletionSource Checked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public int CheckCount { get; private set; }

		public Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
		{
			CheckCount++;
			Checked.TrySetResult();
			return Task.FromResult(new UpdateCheckResult(UpdateAvailability.UpToDate));
		}
	}
}