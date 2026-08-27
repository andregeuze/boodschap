using Boodschap.Features.Updates.Domain;

namespace Boodschap.Features.Updates.Application.Contracts;

public interface IUpdateCheckService
{
	Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

public sealed record UpdateCheckResult(
	UpdateAvailability Availability,
	string? CurrentCommit = null,
	string? LatestCommit = null,
	Uri? LatestCommitUrl = null);