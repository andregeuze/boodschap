using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Infrastructure;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Boodschap.Features.Authentication.Tests;

public sealed class AuthenticationModuleTests
{
	[Fact]
	public void AddAuthenticationFeature_ConfiguresPersistentSlidingCookie()
	{
		var services = new ServiceCollection();
		services.AddLogging();

		services.AddAuthenticationFeature("Data Source=:memory:");

		using var serviceProvider = services.BuildServiceProvider();
		var options = serviceProvider
			.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
			.Get(CookieAuthenticationDefaults.AuthenticationScheme);

		Assert.Equal(LocalAuthenticationDefaults.PersistentSignInLifetime, options.ExpireTimeSpan);
		Assert.True(options.SlidingExpiration);
	}

	[Fact]
	public async Task AddAuthenticationFeature_PersistsDataProtectionKeysAcrossServiceProviders()
	{
		var sqlitePath = Path.Combine(Path.GetTempPath(), $"boodschap-auth-{Guid.NewGuid():N}.db");

		try
		{
			string protectedPayload;

			using (var firstProvider = CreateServiceProvider(sqlitePath))
			{
				await AuthenticationStoreInitializer.InitializeAsync(firstProvider);

				var protector = firstProvider
					.GetRequiredService<IDataProtectionProvider>()
					.CreateProtector("auth-session-persistence");

				protectedPayload = protector.Protect("boodschap");

				await using var dbContext = await firstProvider
					.GetRequiredService<IDbContextFactory<AuthenticationDbContext>>()
					.CreateDbContextAsync();

				Assert.NotEmpty(await dbContext.DataProtectionKeys.AsNoTracking().ToListAsync());
			}

			using var secondProvider = CreateServiceProvider(sqlitePath);
			await AuthenticationStoreInitializer.InitializeAsync(secondProvider);

			var secondProtector = secondProvider
				.GetRequiredService<IDataProtectionProvider>()
				.CreateProtector("auth-session-persistence");

			Assert.Equal("boodschap", secondProtector.Unprotect(protectedPayload));
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