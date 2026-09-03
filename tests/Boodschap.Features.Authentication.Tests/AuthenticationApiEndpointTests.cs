using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
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

	[Fact]
	public async Task ChangePasswordAsync_UsesAuthenticatedUserAsActor()
	{
		var service = new StubAuthenticationService();
		var principal = CreatePrincipal(userId: 12, isAdmin: false);

		var result = await AuthenticationApiEndpoints.ChangePasswordAsync(
			new AuthenticationChangePasswordRequest("current-value", "new-password", "new-password"),
			principal,
			service,
			CancellationToken.None);

		Assert.Equal(StatusCodes.Status204NoContent, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
		Assert.Equal(12, service.PasswordChangeActorId);
	}

	[Fact]
	public async Task CreateUserAsync_UsesAuthenticatedAdminAsActor()
	{
		var service = new StubAuthenticationService();
		var principal = CreatePrincipal(userId: 7, isAdmin: true);

		var result = await AuthenticationApiEndpoints.CreateUserAsync(
			new AuthenticationCreateUserRequest("new-user", "new-password", "new-password", true),
			principal,
			service,
			CancellationToken.None);

		Assert.Equal(StatusCodes.Status204NoContent, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
		Assert.Equal(7, service.CreateUserActorId);
	}

	private static ClaimsPrincipal CreatePrincipal(int userId, bool isAdmin)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, userId.ToString()),
			new(ClaimTypes.Name, "mobile")
		};
		if (isAdmin)
		{
			claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
		}

		return new ClaimsPrincipal(new ClaimsIdentity(claims, "test", ClaimTypes.Name, ClaimTypes.Role));
	}

	private sealed class StubAuthenticationService : ILocalAuthenticationService
	{
		public int LoginCalls { get; private set; }
		public int? PasswordChangeActorId { get; private set; }
		public int? CreateUserActorId { get; private set; }

		public Task<LocalAuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
		{
			LoginCalls++;
			return Task.FromResult(LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.InvalidCredentials));
		}

		public Task<bool> IsBootstrapRegistrationOpenAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public Task<LocalAuthenticationResult> RegisterAsync(string username, string password, string confirmPassword, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public Task<LocalAuthenticationResult> CreateUserAsync(int actorUserId, string username, string password, string confirmPassword, bool isAdmin, CancellationToken cancellationToken = default)
		{
			CreateUserActorId = actorUserId;
			return Task.FromResult(LocalAuthenticationResult.Success(new LocalUser { Username = username, IsAdmin = isAdmin }));
		}

		public Task<LocalPasswordChangeResult> ChangePasswordAsync(int actorUserId, string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
		{
			PasswordChangeActorId = actorUserId;
			return Task.FromResult(LocalPasswordChangeResult.Success());
		}
	}
}