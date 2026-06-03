using Boodschap.Features.Authentication.Domain;

namespace Boodschap.Features.Authentication.Application;

public interface ILocalUserRepository
{
	Task<int> GetUserCountAsync(CancellationToken cancellationToken = default);
	Task<LocalUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
	Task<LocalUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);
	Task<LocalUser> CreateAsync(LocalUser user, CancellationToken cancellationToken = default);
	Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken = default);
}