using Boodschap.Features.Authentication.Domain;

namespace Boodschap.Features.Authentication.Application;

public interface ILocalAuthenticationSession
{
	Task SignInAsync(LocalUser user);
	Task SignOutAsync();
}