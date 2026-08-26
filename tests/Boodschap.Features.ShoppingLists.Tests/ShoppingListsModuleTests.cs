using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class ShoppingListsModuleTests
{
	[Fact]
	public void AddShoppingListsFeature_RegistersShoppingListServices()
	{
		var services = new ServiceCollection();

		services.AddShoppingListsFeature("Data Source=:memory:");

		Assert.Contains(services, service => service.ServiceType == typeof(IShoppingListService));
		Assert.Contains(services, service => service.ServiceType == typeof(IDbContextFactory<BoodschapDbContext>));
	}
}