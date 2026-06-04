namespace Boodschap.Features.Authentication.Application;

public interface ICurrentUserAccessor
{
	Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
	Task<CurrentUser> GetRequiredCurrentUserAsync(CancellationToken cancellationToken = default);
}