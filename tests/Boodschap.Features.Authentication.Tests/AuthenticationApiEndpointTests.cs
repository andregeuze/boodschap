using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Presentation;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Boodschap.Features.Authentication.Tests;

public sealed class AuthenticationApiEndpointTests
{
	[Fact]
	public async Task LoginAsync_WithMissingCredentials_ReturnsBadRequestWithoutCallingService()
	{
		var service = new StubAuthenticationService();

		var result = await AuthenticationApiEndpoints.LoginAsync(
			new AuthenticationLoginRequest(string.Empty, string.Empty),
			service,
			CancellationToken.None);

		Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
		Assert.Equal(0, service.LoginCalls);
	}

	[Fact]
	public async Task LoginAsync_WithInvalidCredentials_ReturnsUnauthorized()
	{
		var service = new StubAuthenticationService();

		var result = await AuthenticationApiEndpoints.LoginAsync(
			new AuthenticationLoginRequest("andre", "incorrect-password"),
			service,
			CancellationToken.None);

		Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
		Assert.Equal(1, service.LoginCalls);
	}

	[Fact]
	public void GetCurrentUser_WithAuthenticatedClaims_ReturnsTransportUser()
	{
		var principal = new ClaimsPrincipal(new ClaimsIdentity(
		[
			new Claim(ClaimTypes.NameIdentifier, "12"),
			new Claim(ClaimTypes.Name, "mobile")
		], "test", ClaimTypes.Name, ClaimTypes.Role));

		var result = AuthenticationApiEndpoints.GetCurrentUser(principal);

		Assert.Equal(StatusCodes.Status200OK, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
		var user = Assert.IsType<AuthenticationUserResponse>(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
		Assert.Equal(12, user.Id);
		Assert.Equal("mobile", user.Username);
	}

	private sealed class StubAuthenticationService : ILocalAuthenticationService
	{
		public int LoginCalls { get; private set; }

		public Task<LocalAuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
		{
			LoginCalls++;
			return Task.FromResult(LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.InvalidCredentials));
		}

		public Task<bool> IsBootstrapRegistrationOpenAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public Task<LocalAuthenticationResult> RegisterAsync(string username, string password, string confirmPassword, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public Task<LocalAuthenticationResult> CreateUserAsync(int actorUserId, string username, string password, string confirmPassword, bool isAdmin, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public Task<LocalPasswordChangeResult> ChangePasswordAsync(int actorUserId, string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}
}