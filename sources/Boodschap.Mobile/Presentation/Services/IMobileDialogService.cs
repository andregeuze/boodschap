namespace Boodschap.Mobile.Presentation.Services;

public interface IMobileDialogService
{
	Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
	Task<string?> PromptAsync(string title, string message, string accept, string cancel, string placeholder, string initialValue);
}