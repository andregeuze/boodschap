using Boodschap.Features.Authentication.Infrastructure;

namespace Boodschap.Features.Authentication.Tests;

public sealed class ReturnUrlSanitizerTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("https://example.com/elsewhere")]
	[InlineData("//example.com/elsewhere")]
	[InlineData("sign-in")]
	public void Normalize_FallsBackToRootForUnsafeOrMissingUrls(string? returnUrl)
	{
		var result = ReturnUrlSanitizer.Normalize(returnUrl);

		Assert.Equal("/", result);
	}

	[Theory]
	[InlineData("/sign-in")]
	[InlineData("/signed-out")]
	[InlineData("/account/login")]
	[InlineData("/account/logout")]
	public void Normalize_FallsBackToRootForAuthenticationRoutes(string returnUrl)
	{
		var result = ReturnUrlSanitizer.Normalize(returnUrl);

		Assert.Equal("/", result);
	}

	[Theory]
	[InlineData("/")]
	[InlineData("/lists/42")]
	[InlineData("/lists/42?filter=Needed")]
	public void Normalize_KeepsLocalApplicationPaths(string returnUrl)
	{
		var result = ReturnUrlSanitizer.Normalize(returnUrl);

		Assert.Equal(returnUrl, result);
	}
}