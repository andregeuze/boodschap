using System.Windows.Input;
using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Updates.Application.Contracts;
using Boodschap.Features.Updates.Domain;
using Boodschap.Shared.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Maui.Graphics;

namespace Boodschap.Mobile.Presentation.ViewModels;

public sealed class AccountViewModel : ObservableObject
{
	private static readonly Color ErrorBackgroundColor = Color.FromArgb("#2A1117");
	private static readonly Color ErrorBorderColor = Color.FromArgb("#7F1D1D");
	private static readonly Color ErrorTextColor = Color.FromArgb("#FECACA");
	private static readonly Color SuccessBackgroundColor = Color.FromArgb("#102A22");
	private static readonly Color SuccessBorderColor = Color.FromArgb("#1E4F42");
	private static readonly Color SuccessTextColor = Color.FromArgb("#D1FAE5");

	private readonly IStringLocalizer<AppStrings> localizer;
	private readonly Func<string, string, string, Task<LocalPasswordChangeResult>> changePasswordAsync;
	private readonly Func<string, string, string, bool, Task<LocalAuthenticationResult>> createUserAsync;
	private readonly IUpdateCheckService updateCheckService;
	private bool isVisible;
	private bool isAdmin;
	private string displayName = string.Empty;
	private string currentPassword = string.Empty;
	private string newPassword = string.Empty;
	private string confirmNewPassword = string.Empty;
	private string passwordStatusMessage = string.Empty;
	private Color passwordStatusBackgroundColor = SuccessBackgroundColor;
	private Color passwordStatusBorderColor = SuccessBorderColor;
	private Color passwordStatusTextColor = SuccessTextColor;
	private string newUserUsername = string.Empty;
	private string newUserPassword = string.Empty;
	private string newUserConfirmPassword = string.Empty;
	private bool newUserIsAdmin;
	private string createUserStatusMessage = string.Empty;
	private Color createUserStatusBackgroundColor = SuccessBackgroundColor;
	private Color createUserStatusBorderColor = SuccessBorderColor;
	private Color createUserStatusTextColor = SuccessTextColor;
	private string updateStatusText;
	private string currentVersion = "-";
	private string remoteVersion = "-";
	private Uri? latestCommitUrl;
	private Color updateStatusBackgroundColor = Color.FromArgb("#0DFFFFFF");
	private Color updateStatusBorderColor = Color.FromArgb("#1AFFFFFF");
	private Color updateStatusTextColor = Color.FromArgb("#D4D4D4");

	public AccountViewModel(
		IStringLocalizer<AppStrings> localizer,
		Func<string, string, string, Task<LocalPasswordChangeResult>> changePasswordAsync,
		Func<string, string, string, bool, Task<LocalAuthenticationResult>> createUserAsync,
		IUpdateCheckService updateCheckService)
	{
		this.localizer = localizer;
		this.changePasswordAsync = changePasswordAsync;
		this.createUserAsync = createUserAsync;
		this.updateCheckService = updateCheckService;
		updateStatusText = localizer["Updates.Checking"].Value;

		Title = localizer["Account.Title"].Value;
		Description = localizer["Account.ManageDescription"].Value;
		UpdatePasswordTitle = localizer["Account.UpdatePassword"].Value;
		UpdatePasswordDescription = localizer["Account.UpdatePasswordDescription"].Value;
		CurrentPasswordLabel = localizer["Account.CurrentPassword"].Value;
		NewPasswordLabel = localizer["Account.NewPassword"].Value;
		ConfirmPasswordLabel = localizer["Account.ConfirmPassword"].Value;
		CreateUserTitle = localizer["Account.CreateUser"].Value;
		CreateUserDescription = localizer["Account.CreateUserDescription"].Value;
		UsernameLabel = localizer["Account.Username"].Value;
		PasswordLabel = localizer["Account.Password"].Value;
		CreateAsAdminLabel = localizer["Account.CreateAsAdmin"].Value;
		UpdateStatusTitle = localizer["Updates.Title"].Value;
		CurrentVersionLabel = localizer["Updates.CurrentVersion"].Value;
		RemoteVersionLabel = localizer["Updates.RemoteVersion"].Value;

		ChangePasswordCommand = new Command(async () => await ChangePasswordAsync());
		CreateUserCommand = new Command(async () => await CreateUserAsync());
		OpenLatestVersionCommand = new Command(async () => await OpenLatestVersionAsync());
	}

	public bool IsVisible
	{
		get => isVisible;
		set => SetProperty(ref isVisible, value);
	}

	public bool IsAdmin
	{
		get => isAdmin;
		private set => SetProperty(ref isAdmin, value);
	}

	public string DisplayName
	{
		get => displayName;
		private set => SetProperty(ref displayName, value);
	}

	public string Title { get; }
	public string Description { get; }
	public string UpdatePasswordTitle { get; }
	public string UpdatePasswordDescription { get; }
	public string CurrentPasswordLabel { get; }
	public string NewPasswordLabel { get; }
	public string ConfirmPasswordLabel { get; }
	public string CreateUserTitle { get; }
	public string CreateUserDescription { get; }
	public string UsernameLabel { get; }
	public string PasswordLabel { get; }
	public string CreateAsAdminLabel { get; }
	public string UpdateStatusTitle { get; }
	public string CurrentVersionLabel { get; }
	public string RemoteVersionLabel { get; }
	public ICommand ChangePasswordCommand { get; }
	public ICommand CreateUserCommand { get; }
	public ICommand OpenLatestVersionCommand { get; }

	public string CurrentPassword
	{
		get => currentPassword;
		set => SetProperty(ref currentPassword, value);
	}

	public string NewPassword
	{
		get => newPassword;
		set => SetProperty(ref newPassword, value);
	}

	public string ConfirmNewPassword
	{
		get => confirmNewPassword;
		set => SetProperty(ref confirmNewPassword, value);
	}

	public string PasswordStatusMessage
	{
		get => passwordStatusMessage;
		private set
		{
			if (SetProperty(ref passwordStatusMessage, value))
			{
				OnPropertyChanged(nameof(IsPasswordStatusVisible));
			}
		}
	}

	public bool IsPasswordStatusVisible => !string.IsNullOrWhiteSpace(PasswordStatusMessage);

	public Color PasswordStatusBackgroundColor
	{
		get => passwordStatusBackgroundColor;
		private set => SetProperty(ref passwordStatusBackgroundColor, value);
	}

	public Color PasswordStatusBorderColor
	{
		get => passwordStatusBorderColor;
		private set => SetProperty(ref passwordStatusBorderColor, value);
	}

	public Color PasswordStatusTextColor
	{
		get => passwordStatusTextColor;
		private set => SetProperty(ref passwordStatusTextColor, value);
	}

	public string NewUserUsername
	{
		get => newUserUsername;
		set => SetProperty(ref newUserUsername, value);
	}

	public string NewUserPassword
	{
		get => newUserPassword;
		set => SetProperty(ref newUserPassword, value);
	}

	public string NewUserConfirmPassword
	{
		get => newUserConfirmPassword;
		set => SetProperty(ref newUserConfirmPassword, value);
	}

	public bool NewUserIsAdmin
	{
		get => newUserIsAdmin;
		set => SetProperty(ref newUserIsAdmin, value);
	}

	public string CreateUserStatusMessage
	{
		get => createUserStatusMessage;
		private set
		{
			if (SetProperty(ref createUserStatusMessage, value))
			{
				OnPropertyChanged(nameof(IsCreateUserStatusVisible));
			}
		}
	}

	public bool IsCreateUserStatusVisible => !string.IsNullOrWhiteSpace(CreateUserStatusMessage);

	public Color CreateUserStatusBackgroundColor
	{
		get => createUserStatusBackgroundColor;
		private set => SetProperty(ref createUserStatusBackgroundColor, value);
	}

	public Color CreateUserStatusBorderColor
	{
		get => createUserStatusBorderColor;
		private set => SetProperty(ref createUserStatusBorderColor, value);
	}

	public Color CreateUserStatusTextColor
	{
		get => createUserStatusTextColor;
		private set => SetProperty(ref createUserStatusTextColor, value);
	}

	public string UpdateStatusText
	{
		get => updateStatusText;
		private set => SetProperty(ref updateStatusText, value);
	}

	public string CurrentVersion
	{
		get => currentVersion;
		private set => SetProperty(ref currentVersion, value);
	}

	public string RemoteVersion
	{
		get => remoteVersion;
		private set => SetProperty(ref remoteVersion, value);
	}

	public bool HasLatestVersionLink => latestCommitUrl is not null;

	public Color UpdateStatusBackgroundColor
	{
		get => updateStatusBackgroundColor;
		private set => SetProperty(ref updateStatusBackgroundColor, value);
	}

	public Color UpdateStatusBorderColor
	{
		get => updateStatusBorderColor;
		private set => SetProperty(ref updateStatusBorderColor, value);
	}

	public Color UpdateStatusTextColor
	{
		get => updateStatusTextColor;
		private set => SetProperty(ref updateStatusTextColor, value);
	}

	public void SetUser(LocalUser user)
	{
		DisplayName = user.Username;
		IsAdmin = user.IsAdmin;
		ClearForms();
	}

	public async Task RefreshUpdateStatusAsync()
	{
		UpdateStatusText = localizer["Updates.Checking"].Value;
		var result = await updateCheckService.CheckAsync();
		CurrentVersion = ShortCommit(result.CurrentCommit);
		RemoteVersion = ShortCommit(result.LatestCommit);
		latestCommitUrl = result.LatestCommitUrl;
		OnPropertyChanged(nameof(HasLatestVersionLink));

		UpdateStatusText = result.Availability switch
		{
			UpdateAvailability.UpToDate => localizer["Updates.UpToDate"].Value,
			UpdateAvailability.UpdateAvailable => localizer["Updates.Available"].Value,
			_ => localizer["Updates.Unavailable"].Value
		};

		UpdateStatusBackgroundColor = result.Availability switch
		{
			UpdateAvailability.UpToDate => Color.FromArgb("#102A22"),
			UpdateAvailability.UpdateAvailable => Color.FromArgb("#33250A"),
			_ => Color.FromArgb("#0DFFFFFF")
		};
		UpdateStatusBorderColor = result.Availability switch
		{
			UpdateAvailability.UpToDate => Color.FromArgb("#1E4F42"),
			UpdateAvailability.UpdateAvailable => Color.FromArgb("#785A16"),
			_ => Color.FromArgb("#1AFFFFFF")
		};
		UpdateStatusTextColor = result.Availability switch
		{
			UpdateAvailability.UpToDate => Color.FromArgb("#A7F3D0"),
			UpdateAvailability.UpdateAvailable => Color.FromArgb("#FCD34D"),
			_ => Color.FromArgb("#A3A3A3")
		};
	}

	public void ClearForms()
	{
		CurrentPassword = string.Empty;
		NewPassword = string.Empty;
		ConfirmNewPassword = string.Empty;
		PasswordStatusMessage = string.Empty;
		NewUserUsername = string.Empty;
		NewUserPassword = string.Empty;
		NewUserConfirmPassword = string.Empty;
		NewUserIsAdmin = false;
		CreateUserStatusMessage = string.Empty;
	}

	private async Task ChangePasswordAsync()
	{
		var result = await changePasswordAsync(CurrentPassword, NewPassword, ConfirmNewPassword);
		SetPasswordStatus(
			result.Succeeded ? localizer["Account.PasswordUpdated"].Value : DescribeError(result.ErrorCode),
			isError: !result.Succeeded);

		if (result.Succeeded)
		{
			CurrentPassword = string.Empty;
			NewPassword = string.Empty;
			ConfirmNewPassword = string.Empty;
		}
	}

	private async Task CreateUserAsync()
	{
		var result = await createUserAsync(NewUserUsername.Trim(), NewUserPassword, NewUserConfirmPassword, NewUserIsAdmin);
		SetCreateUserStatus(
			result.Succeeded
				? localizer[NewUserIsAdmin ? "Account.AdminAccountCreated" : "Account.UserAccountCreated"].Value
				: DescribeError(result.ErrorCode),
			isError: !result.Succeeded);

		if (result.Succeeded)
		{
			NewUserUsername = string.Empty;
			NewUserPassword = string.Empty;
			NewUserConfirmPassword = string.Empty;
			NewUserIsAdmin = false;
		}
	}

	private void SetPasswordStatus(string message, bool isError)
	{
		PasswordStatusBackgroundColor = isError ? ErrorBackgroundColor : SuccessBackgroundColor;
		PasswordStatusBorderColor = isError ? ErrorBorderColor : SuccessBorderColor;
		PasswordStatusTextColor = isError ? ErrorTextColor : SuccessTextColor;
		PasswordStatusMessage = message;
	}

	private void SetCreateUserStatus(string message, bool isError)
	{
		CreateUserStatusBackgroundColor = isError ? ErrorBackgroundColor : SuccessBackgroundColor;
		CreateUserStatusBorderColor = isError ? ErrorBorderColor : SuccessBorderColor;
		CreateUserStatusTextColor = isError ? ErrorTextColor : SuccessTextColor;
		CreateUserStatusMessage = message;
	}

	private string DescribeError(string? errorCode)
	{
		return errorCode switch
		{
			LocalAuthenticationErrorCodes.AdminRequired => localizer["Account.Error.AdminRequired"].Value,
			LocalAuthenticationErrorCodes.BootstrapRegistrationClosed => localizer["Account.Error.BootstrapRegistrationClosed"].Value,
			LocalAuthenticationErrorCodes.CurrentPasswordInvalid => localizer["Account.Error.CurrentPasswordInvalid"].Value,
			LocalAuthenticationErrorCodes.CurrentPasswordRequired => localizer["Account.Error.CurrentPasswordRequired"].Value,
			LocalAuthenticationErrorCodes.UsernameRequired => localizer["Account.Error.UsernameRequired"].Value,
			LocalAuthenticationErrorCodes.PasswordRequired => localizer["Account.Error.PasswordRequired"].Value,
			LocalAuthenticationErrorCodes.PasswordTooShort => localizer["Account.Error.PasswordTooShort"].Value,
			LocalAuthenticationErrorCodes.PasswordMismatch => localizer["Account.Error.PasswordMismatch"].Value,
			LocalAuthenticationErrorCodes.UsernameTaken => localizer["Account.Error.UsernameTaken"].Value,
			_ => localizer["Account.Error.RequestFailed"].Value
		};
	}

	private async Task OpenLatestVersionAsync()
	{
		if (latestCommitUrl is not null)
		{
			await Launcher.Default.OpenAsync(latestCommitUrl);
		}
	}

	private static string ShortCommit(string? commit)
	{
		return string.IsNullOrWhiteSpace(commit) ? "-" : commit[..Math.Min(7, commit.Length)];
	}
}