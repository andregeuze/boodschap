using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Authentication.Infrastructure.Persistence;

public sealed class LocalUserRepository(IDbContextFactory<AuthenticationDbContext> dbContextFactory) : ILocalUserRepository
{
	public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.LocalUsers.CountAsync(cancellationToken);
	}

	public async Task<LocalUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.LocalUsers
			.AsNoTracking()
			.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
	}

	public async Task<LocalUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.LocalUsers
			.AsNoTracking()
			.SingleOrDefaultAsync(user => user.NormalizedUsername == normalizedUsername, cancellationToken);
	}

	public async Task<LocalUser> CreateAsync(LocalUser user, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		dbContext.LocalUsers.Add(user);
		await dbContext.SaveChangesAsync(cancellationToken);
		return user;
	}

	public async Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var user = await dbContext.LocalUsers.SingleOrDefaultAsync(entry => entry.Id == userId, cancellationToken);
		if (user is null)
		{
			return;
		}

		user.PasswordHash = passwordHash;
		await dbContext.SaveChangesAsync(cancellationToken);
	}
}