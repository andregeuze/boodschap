using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Nutrition.Infrastructure.Persistence;

public static class NutritionInitializer
{
	public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
	{
		await using var scope = services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<NutritionDbContext>>();
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		await dbContext.Database.MigrateAsync(cancellationToken);
	}
}