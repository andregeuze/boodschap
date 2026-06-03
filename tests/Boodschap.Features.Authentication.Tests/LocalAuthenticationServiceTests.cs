using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Microsoft.AspNetCore.Identity;

namespace Boodschap.Features.Authentication.Tests;

public sealed class LocalAuthenticationServiceTests
{
	[Fact]
	public async Task RegisterAsync_FirstUserBecomesAdmin()
	{
		var repository = new FakeLocalUserRepository();
		var service = new LocalAuthenticationService(repository, new PasswordHasher<LocalUser>());

		var result = await service.RegisterAsync("andre", "secret123", "secret123");

		Assert.True(result.Succeeded);
		Assert.NotNull(result.User);
		Assert.Equal("andre", result.User.Username);
		Assert.True(result.User.IsAdmin);
		Assert.NotEqual("secret123", result.User.PasswordHash);
		Assert.Single(repository.Users);
	}

	[Fact]
	public async Task RegisterAsync_FailsWhenBootstrapAlreadyClosed()
	{
		var repository = new FakeLocalUserRepository();
		var hasher = new PasswordHasher<LocalUser>();
		repository.Users.Add(new LocalUser
		{
			Id = 1,
			Username = "Andre",
			NormalizedUsername = "ANDRE",
			IsAdmin = true,
			PasswordHash = hasher.HashPassword(new LocalUser { Username = "Andre", NormalizedUsername = "ANDRE" }, "secret123"),
			CreatedUtc = DateTimeOffset.UtcNow
		});
		var service = new LocalAuthenticationService(repository, hasher);

		var result = await service.RegisterAsync("andre", "secret123", "secret123");

		Assert.False(result.Succeeded);
		Assert.Equal(LocalAuthenticationErrorCodes.BootstrapRegistrationClosed, result.ErrorCode);
	}

	[Fact]
	public async Task CreateUserAsync_RequiresAdminActor()
	{
		var repository = new FakeLocalUserRepository();
		var hasher = new PasswordHasher<LocalUser>();
		repository.Users.Add(new LocalUser
		{
			Id = 1,
			Username = "andre",
			NormalizedUsername = "ANDRE",
			IsAdmin = false,
			PasswordHash = hasher.HashPassword(new LocalUser { Username = "andre", NormalizedUsername = "ANDRE" }, "secret123"),
			CreatedUtc = DateTimeOffset.UtcNow
		});
		var service = new LocalAuthenticationService(repository, hasher);

		var result = await service.CreateUserAsync(1, "pat", "secret123", "secret123", isAdmin: false);

		Assert.False(result.Succeeded);
		Assert.Equal(LocalAuthenticationErrorCodes.AdminRequired, result.ErrorCode);
		Assert.Single(repository.Users);
	}

	[Fact]
	public async Task CreateUserAsync_AdminCanCreateAnotherAdministrator()
	{
		var repository = new FakeLocalUserRepository();
		var hasher = new PasswordHasher<LocalUser>();
		repository.Users.Add(new LocalUser
		{
			Id = 1,
			Username = "andre",
			NormalizedUsername = "ANDRE",
			IsAdmin = true,
			PasswordHash = hasher.HashPassword(new LocalUser { Username = "andre", NormalizedUsername = "ANDRE" }, "secret123"),
			CreatedUtc = DateTimeOffset.UtcNow
		});
		var service = new LocalAuthenticationService(repository, hasher);

		var result = await service.CreateUserAsync(1, "pat", "secret123", "secret123", isAdmin: true);

		Assert.True(result.Succeeded);
		Assert.NotNull(result.User);
		Assert.True(result.User.IsAdmin);
		Assert.Equal(2, repository.Users.Count);
	}

	[Fact]
	public async Task ChangePasswordAsync_RejectsWrongCurrentPassword()
	{
		var repository = new FakeLocalUserRepository();
		var hasher = new PasswordHasher<LocalUser>();
		var user = new LocalUser
		{
			Id = 1,
			Username = "andre",
			NormalizedUsername = "ANDRE",
			CreatedUtc = DateTimeOffset.UtcNow
		};
		user.PasswordHash = hasher.HashPassword(user, "secret123");
		repository.Users.Add(user);
		var service = new LocalAuthenticationService(repository, hasher);

		var result = await service.ChangePasswordAsync(1, "wrong-password", "newsecret123", "newsecret123");

		Assert.False(result.Succeeded);
		Assert.Equal(LocalAuthenticationErrorCodes.CurrentPasswordInvalid, result.ErrorCode);
	}

	[Fact]
	public async Task ChangePasswordAsync_UpdatesStoredPasswordHash()
	{
		var repository = new FakeLocalUserRepository();
		var hasher = new PasswordHasher<LocalUser>();
		var user = new LocalUser
		{
			Id = 1,
			Username = "andre",
			NormalizedUsername = "ANDRE",
			CreatedUtc = DateTimeOffset.UtcNow
		};
		user.PasswordHash = hasher.HashPassword(user, "secret123");
		repository.Users.Add(user);
		var originalHash = user.PasswordHash;
		var service = new LocalAuthenticationService(repository, hasher);

		var result = await service.ChangePasswordAsync(1, "secret123", "newsecret123", "newsecret123");

		Assert.True(result.Succeeded);
		Assert.NotEqual(originalHash, repository.Users.Single().PasswordHash);
	}

	[Fact]
	public async Task LoginAsync_ReturnsInvalidCredentialsForWrongPassword()
	{
		var repository = new FakeLocalUserRepository();
		var hasher = new PasswordHasher<LocalUser>();
		var user = new LocalUser
		{
			Id = 1,
			Username = "andre",
			NormalizedUsername = "ANDRE",
			CreatedUtc = DateTimeOffset.UtcNow
		};
		user.PasswordHash = hasher.HashPassword(user, "secret123");
		repository.Users.Add(user);
		var service = new LocalAuthenticationService(repository, hasher);

		var result = await service.LoginAsync("andre", "wrong-password");

		Assert.False(result.Succeeded);
		Assert.Equal(LocalAuthenticationErrorCodes.InvalidCredentials, result.ErrorCode);
	}

	[Fact]
	public async Task LoginAsync_ReturnsUserForValidCredentials()
	{
		var repository = new FakeLocalUserRepository();
		var hasher = new PasswordHasher<LocalUser>();
		var user = new LocalUser
		{
			Id = 7,
			Username = "andre",
			NormalizedUsername = "ANDRE",
			CreatedUtc = DateTimeOffset.UtcNow
		};
		user.PasswordHash = hasher.HashPassword(user, "secret123");
		repository.Users.Add(user);
		var service = new LocalAuthenticationService(repository, hasher);

		var result = await service.LoginAsync(" Andre ", "secret123");

		Assert.True(result.Succeeded);
		Assert.NotNull(result.User);
		Assert.Equal(7, result.User.Id);
	}

	private sealed class FakeLocalUserRepository : ILocalUserRepository
	{
		public List<LocalUser> Users { get; } = [];

		public Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Users.Count);
		}

		public Task<LocalUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Users.SingleOrDefault(user => user.Id == id));
		}

		public Task<LocalUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Users.SingleOrDefault(user => user.NormalizedUsername == normalizedUsername));
		}

		public Task<LocalUser> CreateAsync(LocalUser user, CancellationToken cancellationToken = default)
		{
			user.Id = Users.Count + 1;
			Users.Add(user);
			return Task.FromResult(user);
		}

		public Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken = default)
		{
			var user = Users.Single(entry => entry.Id == userId);
			user.PasswordHash = passwordHash;
			return Task.CompletedTask;
		}
	}
}