using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Presentation.Components;
using Boodschap.Features.Updates;
using Boodschap.Features.Updates.Presentation.Components;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using AccountPage = Boodschap.Components.Pages.Account;

namespace Boodschap.Tests;

public sealed class AccountPageCompositionTests
{
	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public void Render_ComposesAuthenticationWithOptionalUpdateStatus(bool updatesEnabled, int expectedUpdateStatuses)
	{
		using var context = new BunitContext();
		context.Services.AddLocalization(options => options.ResourcesPath = "Resources");
		context.Services.Configure<UpdateFeatureOptions>(options => options.Enabled = updatesEnabled);
		context.Services.AddSingleton<ICurrentUserAccessor>(new StubCurrentUserAccessor());
		context.Services.AddSingleton<ILocalAuthenticationService>(new StubLocalAuthenticationService());
		context.ComponentFactories.AddStub<UpdateStatus>();

		var cut = context.Render<AccountPage>();

		Assert.Single(cut.FindComponents<AccountSettings>());
		Assert.Equal(expectedUpdateStatuses, cut.FindComponents<Stub<UpdateStatus>>().Count);
	}

	private sealed class StubCurrentUserAccessor : ICurrentUserAccessor
	{
		private static readonly CurrentUser User = new(1, "1", "Test User", null, IsAdmin: false);

		public Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<CurrentUser?>(User);
		}

		public Task<CurrentUser> GetRequiredCurrentUserAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(User);
		}
	}

	private sealed class StubLocalAuthenticationService : ILocalAuthenticationService
	{
		public Task<LocalAuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(LocalAuthenticationResult.Failure("not-used"));
		}

		public Task<bool> IsBootstrapRegistrationOpenAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(false);
		}

		public Task<LocalAuthenticationResult> RegisterAsync(string username, string password, string confirmPassword, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(LocalAuthenticationResult.Failure("not-used"));
		}

		public Task<LocalAuthenticationResult> CreateUserAsync(int actorUserId, string username, string password, string confirmPassword, bool isAdmin, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(LocalAuthenticationResult.Failure("not-used"));
		}

		public Task<LocalPasswordChangeResult> ChangePasswordAsync(int actorUserId, string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(LocalPasswordChangeResult.Failure("not-used"));
		}
	}
}