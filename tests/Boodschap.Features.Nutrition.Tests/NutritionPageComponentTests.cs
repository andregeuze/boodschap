using System.Globalization;
using Bunit;
using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Nutrition.Application;
using Boodschap.Features.Nutrition.Domain;
using Boodschap.Features.Nutrition.Presentation.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Nutrition.Tests;

public sealed class NutritionPageComponentTests
{
	[Fact]
	public void Render_ShowsImportControlForAdmins()
	{
		using var context = CreateContext(isAdmin: true);

		var cut = context.Render<NutritionPage>();

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll("input[type='file'][aria-label='NEVO-data importeren']")));
	}

	[Fact]
	public void Render_HidesImportControlForNonAdmins()
	{
		using var context = CreateContext(isAdmin: false);

		var cut = context.Render<NutritionPage>();

		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("input[type='file']")));
	}

	private static BunitContext CreateContext(bool isAdmin)
	{
		var culture = CultureInfo.GetCultureInfo("nl-NL");
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;

		var context = new BunitContext();
		context.Services.AddLocalization(options => options.ResourcesPath = "Resources");
		context.Services.AddSingleton<IFoodService>(new FakeFoodService());
		context.Services.AddSingleton<ICurrentUserAccessor>(new FakeCurrentUserAccessor(isAdmin));
		return context;
	}

	private sealed class FakeCurrentUserAccessor(bool isAdmin) : ICurrentUserAccessor
	{
		private readonly CurrentUser currentUser = new(
			LocalUserId: 1,
			Id: "local:1",
			DisplayName: isAdmin ? "Admin" : "User",
			Email: null,
			IsAdmin: isAdmin);

		public Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<CurrentUser?>(currentUser);
		}

		public Task<CurrentUser> GetRequiredCurrentUserAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(currentUser);
		}
	}

	private sealed class FakeFoodService : IFoodService
	{
		public Task<IReadOnlyList<Food>> GetFoodsAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IReadOnlyList<Food>>([]);
		}

		public Task<IReadOnlyList<Food>> SearchFoodsAsync(string query, CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IReadOnlyList<Food>>([]);
		}

		public Task<FoodImportResult> ImportNevoDetailsAsync(Stream source, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new FoodImportResult(0));
		}
	}
}