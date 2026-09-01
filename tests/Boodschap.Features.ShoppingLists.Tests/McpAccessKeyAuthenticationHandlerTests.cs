using Boodschap.Features.ShoppingLists.Infrastructure.Mcp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class McpAccessKeyAuthenticationHandlerTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData("wrong-key", false)]
	[InlineData("configured-key", true)]
	public async Task AuthenticateAsync_RequiresConfiguredAccessKey(string? providedAccessKey, bool expectedSuccess)
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				[ShoppingListsMcpDefaults.AccessKeyConfigurationKey] = "configured-key"
			})
			.Build();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddAuthentication()
			.AddScheme<AuthenticationSchemeOptions, McpAccessKeyAuthenticationHandler>(
				ShoppingListsMcpDefaults.AuthenticationScheme,
				_ => { });
		await using var serviceProvider = services.BuildServiceProvider();
		var context = new DefaultHttpContext { RequestServices = serviceProvider };
		if (providedAccessKey is not null)
		{
			context.Request.Headers.Authorization = $"Bearer {providedAccessKey}";
		}

		var result = await context.AuthenticateAsync(ShoppingListsMcpDefaults.AuthenticationScheme);

		Assert.Equal(expectedSuccess, result.Succeeded);
		if (expectedSuccess)
		{
			Assert.Equal("GitHub Copilot", result.Principal?.Identity?.Name);
		}
	}

	[Fact]
	public async Task AuthenticateAsync_WithoutConfiguredAccessKey_FailsClosed()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
		services.AddAuthentication()
			.AddScheme<AuthenticationSchemeOptions, McpAccessKeyAuthenticationHandler>(
				ShoppingListsMcpDefaults.AuthenticationScheme,
				_ => { });
		await using var serviceProvider = services.BuildServiceProvider();
		var context = new DefaultHttpContext { RequestServices = serviceProvider };
		context.Request.Headers.Authorization = "Bearer any-key";

		var result = await context.AuthenticateAsync(ShoppingListsMcpDefaults.AuthenticationScheme);

		Assert.False(result.Succeeded);
		Assert.NotNull(result.Failure);
	}
}