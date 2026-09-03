using Microsoft.Maui.ApplicationModel;

namespace Boodschap.Mobile.Presentation.Services;

public sealed class PageDialogService : IMobileDialogService
{
	public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
	{
		return MainThread.InvokeOnMainThreadAsync(() => GetCurrentPage().DisplayAlertAsync(title, message, accept, cancel));
	}

	public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string placeholder, string initialValue)
	{
		return MainThread.InvokeOnMainThreadAsync(() => GetCurrentPage().DisplayPromptAsync(
			title,
			message,
			accept: accept,
			cancel: cancel,
			placeholder: placeholder,
			initialValue: initialValue));
	}

	private static Page GetCurrentPage()
	{
		var page = Application.Current?.Windows.FirstOrDefault()?.Page
			?? throw new InvalidOperationException("A visible page is required before showing a dialog.");

		while (page is NavigationPage navigationPage)
		{
			page = navigationPage.CurrentPage;
		}

		return page;
	}
}