using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace Boodschap.Features.ShoppingLists.Infrastructure.Mcp;

public static class ShoppingListsMcpDefaults
{
	public const string AuthenticationScheme = "BoodschapMcp";
	public const string AccessKeyConfigurationKey = "Mcp:AccessKey";
	public const string Route = "/mcp";
}

internal sealed class McpAccessKeyAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder,
	IConfiguration configuration)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		var expectedAccessKey = configuration[ShoppingListsMcpDefaults.AccessKeyConfigurationKey];
		if (string.IsNullOrWhiteSpace(expectedAccessKey))
		{
			return Task.FromResult(AuthenticateResult.Fail("MCP access is not configured."));
		}

		var authorization = Request.Headers.Authorization.ToString();
		const string bearerPrefix = "Bearer ";
		if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var providedAccessKey = authorization[bearerPrefix.Length..].Trim();
		if (!AccessKeysMatch(expectedAccessKey, providedAccessKey))
		{
			return Task.FromResult(AuthenticateResult.Fail("The MCP access key is invalid."));
		}

		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, "GitHub Copilot")],
			ShoppingListsMcpDefaults.AuthenticationScheme);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, ShoppingListsMcpDefaults.AuthenticationScheme);

		return Task.FromResult(AuthenticateResult.Success(ticket));
	}

	protected override Task HandleChallengeAsync(AuthenticationProperties properties)
	{
		Response.Headers.WWWAuthenticate = "Bearer";
		return base.HandleChallengeAsync(properties);
	}

	private static bool AccessKeysMatch(string expectedAccessKey, string providedAccessKey)
	{
		var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedAccessKey));
		var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedAccessKey));
		return CryptographicOperations.FixedTimeEquals(expectedHash, providedHash);
	}
}