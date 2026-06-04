using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Boodschap.Features.Authentication.Tests.Testing;

namespace Boodschap.Features.Authentication.Tests;

public sealed class LocalUserRepositoryTests
{
	[Fact]
	public async Task CreateAsync_PersistsUserAndAssignsId()
	{
		await using var harness = await AuthenticationSqliteTestHarness.CreateAsync();
		var repository = new LocalUserRepository(harness.DbContextFactory);

		var createdUser = await repository.CreateAsync(new LocalUser
		{
			Username = "andre",
			NormalizedUsername = "ANDRE",
			PasswordHash = "hash-1",
			IsAdmin = true,
			CreatedUtc = DateTimeOffset.UtcNow
		});

		Assert.True(createdUser.Id > 0);
		var storedUser = await harness.GetUserAsync(createdUser.Id);
		Assert.NotNull(storedUser);
		Assert.Equal("andre", storedUser.Username);
		Assert.True(storedUser.IsAdmin);
	}

	[Fact]
	public async Task GetByNormalizedUsernameAsync_ReturnsPersistedUser()
	{
		await using var harness = await AuthenticationSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(new LocalUser
		{
			Username = "andre",
			NormalizedUsername = "ANDRE",
			PasswordHash = "hash-1",
			CreatedUtc = DateTimeOffset.UtcNow
		});

		var repository = new LocalUserRepository(harness.DbContextFactory);

		var user = await repository.GetByNormalizedUsernameAsync("ANDRE");

		Assert.NotNull(user);
		Assert.Equal("andre", user.Username);
	}

	[Fact]
	public async Task UpdatePasswordHashAsync_UpdatesStoredPasswordHash()
	{
		await using var harness = await AuthenticationSqliteTestHarness.CreateAsync();
		await harness.SeedAsync(new LocalUser
		{
			Id = 7,
			Username = "andre",
			NormalizedUsername = "ANDRE",
			PasswordHash = "hash-1",
			CreatedUtc = DateTimeOffset.UtcNow
		});

		var repository = new LocalUserRepository(harness.DbContextFactory);

		await repository.UpdatePasswordHashAsync(7, "hash-2");

		var user = await harness.GetUserAsync(7);
		Assert.NotNull(user);
		Assert.Equal("hash-2", user.PasswordHash);
	}
}