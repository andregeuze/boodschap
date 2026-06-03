namespace Boodschap.Features.Authentication.Infrastructure;

internal static class ReturnUrlSanitizer
{
	public static string Normalize(string? returnUrl)
	{
		if (string.IsNullOrWhiteSpace(returnUrl))
		{
			return "/";
		}

		if (!returnUrl.StartsWith("/", StringComparison.Ordinal) || returnUrl.StartsWith("//", StringComparison.Ordinal))
		{
			return "/";
		}

		if (returnUrl.StartsWith("/account/", StringComparison.OrdinalIgnoreCase)
			|| returnUrl.StartsWith("/sign-in", StringComparison.OrdinalIgnoreCase)
			|| returnUrl.StartsWith("/signed-out", StringComparison.OrdinalIgnoreCase))
		{
			return "/";
		}

		return returnUrl;
	}
}