using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Microsoft.AspNetCore.Identity;

namespace Boodschap.Features.Authentication.Infrastructure.Persistence;

public static class AuthenticationDevelopmentSeeder
{
	public const string DevelopmentUsername = "Geuze";
	public const string DevelopmentPassword = "Welkom01";

	private const string NormalizedDevelopmentUsername = "GEUZE";

	public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
	{
		await using var scope = services.CreateAsyncScope();
		var userRepository = scope.ServiceProvider.GetRequiredService<ILocalUserRepository>();
		if (await userRepository.GetByNormalizedUsernameAsync(NormalizedDevelopmentUsername, cancellationToken) is not null)
		{
			return;
		}

		var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<LocalUser>>();
		var user = new LocalUser
		{
			Username = DevelopmentUsername,
			NormalizedUsername = NormalizedDevelopmentUsername,
			IsAdmin = true,
			CreatedUtc = DateTimeOffset.UtcNow
		};
		user.PasswordHash = passwordHasher.HashPassword(user, DevelopmentPassword);

		await userRepository.CreateAsync(user, cancellationToken);
	}
}