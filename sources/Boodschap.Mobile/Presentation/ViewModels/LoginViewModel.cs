using System.Windows.Input;
using Boodschap.Shared.Localization;
using Microsoft.Extensions.Localization;

namespace Boodschap.Mobile.Presentation.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
	private readonly Func<string, string, Task> signInAsync;
	private string username = string.Empty;
	private string password = string.Empty;
	private bool isVisible;

	public LoginViewModel(IStringLocalizer<AppStrings> localizer, Func<string, string, Task> signInAsync)
	{
		this.signInAsync = signInAsync;
		Eyebrow = localizer["Account.AccessEyebrow"].Value;
		Title = localizer["Account.DefaultTitle"].Value;
		Description = localizer["Account.DefaultDescription"].Value;
		FormTitle = localizer["Account.SignIn"].Value;
		FormDescription = localizer["Account.SignInDescription"].Value;
		RegisterClosedMessage = localizer["Account.RegisterClosed"].Value;
		UsernameLabel = localizer["Account.Username"].Value;
		PasswordLabel = localizer["Account.Password"].Value;
		LoginText = localizer["Account.SignIn"].Value;
		SignInCommand = new Command(async () => await this.signInAsync(Username.Trim(), Password));
	}

	public bool IsVisible
	{
		get => isVisible;
		set => SetProperty(ref isVisible, value);
	}

	public string Eyebrow { get; }

	public string Title { get; }

	public string Description { get; }

	public string FormTitle { get; }

	public string FormDescription { get; }

	public string RegisterClosedMessage { get; }

	public string UsernameLabel { get; }

	public string PasswordLabel { get; }

	public string LoginText { get; }

	public ICommand SignInCommand { get; }

	public string Username
	{
		get => username;
		set => SetProperty(ref username, value);
	}

	public string Password
	{
		get => password;
		set => SetProperty(ref password, value);
	}

	public void ClearPassword()
	{
		Password = string.Empty;
	}
}