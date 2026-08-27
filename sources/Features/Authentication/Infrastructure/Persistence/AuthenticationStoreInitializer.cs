using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Authentication.Infrastructure.Persistence;

public static class AuthenticationStoreInitializer
{
	public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
	{
		await using var scope = services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AuthenticationDbContext>>();
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		await dbContext.Database.MigrateAsync(cancellationToken);
	}
}