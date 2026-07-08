using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Boodschap.Features.Authentication.Tests.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Authentication.Tests;

public sealed class AuthenticationDevelopmentSeederTests
{
	[Fact]
	public async Task SeedAsync_CreatesDefaultUserOnce()
	{
		var sqlitePath = Path.Combine(Path.GetTempPath(), $"boodschap-auth-dev-{Guid.NewGuid():N}.db");

		try
		{
			using var serviceProvider = CreateServiceProvider(sqlitePath);
			await AuthenticationStoreInitializer.InitializeAsync(serviceProvider);

			await AuthenticationDevelopmentSeeder.SeedAsync(serviceProvider);
			await AuthenticationDevelopmentSeeder.SeedAsync(serviceProvider);

			await using var scope = serviceProvider.CreateAsyncScope();
			var authenticationService = scope.ServiceProvider.GetRequiredService<ILocalAuthenticationService>();
			var loginResult = await authenticationService.LoginAsync(AuthenticationDevelopmentSeeder.DevelopmentUsername, AuthenticationDevelopmentSeeder.DevelopmentPassword);

			Assert.True(loginResult.Succeeded);
			Assert.NotNull(loginResult.User);
			Assert.Equal(AuthenticationDevelopmentSeeder.DevelopmentUsername, loginResult.User.Username);
			Assert.True(loginResult.User.IsAdmin);

			var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AuthenticationDbContext>>();
			await using var dbContext = await dbContextFactory.CreateDbContextAsync();
			Assert.Single(await dbContext.LocalUsers.AsNoTracking().ToListAsync());
		}
		finally
		{
			DeleteSqliteArtifacts(sqlitePath);
		}
	}

	private static ServiceProvider CreateServiceProvider(string sqlitePath)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddAuthenticationFeature($"Data Source={sqlitePath}");

		return services.BuildServiceProvider();
	}

	private static void DeleteSqliteArtifacts(string sqlitePath)
	{
		DeleteIfExists(sqlitePath);
		DeleteIfExists($"{sqlitePath}-shm");
		DeleteIfExists($"{sqlitePath}-wal");
	}

	private static void DeleteIfExists(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}

		try
		{
			File.Delete(path);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}