namespace Boodschap.Features.Authentication.Application;

public interface ILocalAuthenticationService
{
	Task<LocalAuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
	Task<bool> IsBootstrapRegistrationOpenAsync(CancellationToken cancellationToken = default);
	Task<LocalAuthenticationResult> RegisterAsync(string username, string password, string confirmPassword, CancellationToken cancellationToken = default);
	Task<LocalAuthenticationResult> CreateUserAsync(int actorUserId, string username, string password, string confirmPassword, bool isAdmin, CancellationToken cancellationToken = default);
	Task<LocalPasswordChangeResult> ChangePasswordAsync(int actorUserId, string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken = default);
}