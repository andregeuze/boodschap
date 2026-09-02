using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Shared.Localization;
using Boodschap.Shared.Realtime;
using AndroidView = Android.Views.View;
using AndroidWindowInsets = Android.Views.WindowInsets;
using Microsoft.Extensions.Localization;
using Microsoft.Maui.ApplicationModel;

namespace Boodschap.Mobile;

public partial class MainPage : ContentPage
{
	private readonly AppInitializationService appInitializationService;
	private readonly ILocalAuthenticationService authenticationService;
	private readonly IShoppingListService shoppingListService;
	private readonly MobileSessionState sessionState;
	private readonly StoreChangeNotifier storeChangeNotifier;
	private readonly IStringLocalizer<AppStrings> localizer;
	private readonly SemaphoreSlim interactionLock = new(1, 1);
	private bool initialized;
	private ShoppingList? currentList;
	private int? currentListId;

	public MainPage(
		AppInitializationService appInitializationService,
		ILocalAuthenticationService authenticationService,
		IShoppingListService shoppingListService,
		MobileSessionState sessionState,
		StoreChangeNotifier storeChangeNotifier,
		IStringLocalizer<AppStrings> localizer)
	{
		InitializeComponent();
		this.appInitializationService = appInitializationService;
		this.authenticationService = authenticationService;
		this.shoppingListService = shoppingListService;
		this.sessionState = sessionState;
		this.storeChangeNotifier = storeChangeNotifier;
		this.localizer = localizer;

		BindingContext = this;
		ConfigureStaticText();
		UpdateChrome();
		ShowLoginView();

		sessionState.Changed += HandleSessionChangedAsync;
		storeChangeNotifier.Changed += HandleStoreChangedAsync;
	}

	public string EditButtonText => localizer["Common.Edit"].Value;
	public string ArchiveButtonText => localizer["Common.Archive"].Value;
	public string UnarchiveButtonText => localizer["Common.Unarchive"].Value;
	public string RemoveButtonText => localizer["Common.Remove"].Value;
	public string MoveUpButtonText => localizer["Common.Up"].Value;
	public string MoveDownButtonText => localizer["Common.Down"].Value;

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = EnsureInitializedAsync();
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();

		if (Handler?.PlatformView is not AndroidView platformView)
		{
			return;
		}

		ApplyAccessibilityLabels();
		platformView.SetOnApplyWindowInsetsListener(new SystemBarInsetsListener(TopBar));
		platformView.RequestApplyInsets();
	}

	private async Task EnsureInitializedAsync()
	{
		if (initialized)
		{
			return;
		}

		SetBusy(true);
		try
		{
			await appInitializationService.InitializeAsync();
			initialized = true;
			await RefreshForCurrentStateAsync();
		}
		catch
		{
			ShowStatus(localizer["Account.Error.RequestFailed"], isError: true);
			ShowLoginView();
		}
		finally
		{
			SetBusy(false);
		}
	}

	private async void HandleHomeTapped(object? sender, TappedEventArgs e)
	{
		if (sessionState.CurrentUser is null || currentListId is null)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			currentListId = null;
			currentList = null;
			await LoadOverviewAsync();
		});
	}

	private async void HandleSignOutClicked(object? sender, EventArgs e)
	{
		await RunBusyActionAsync(async () =>
		{
			ClearStatus();
			await sessionState.SignOutAsync();
		});
	}

	private async void HandleLoginClicked(object? sender, EventArgs e)
	{
		var username = UsernameEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text ?? string.Empty;

		await RunBusyActionAsync(async () =>
		{
			var result = await authenticationService.LoginAsync(username, password);
			if (!result.Succeeded || result.User is null)
			{
				ShowStatus(GetAuthenticationErrorMessage(result.ErrorCode), isError: true);
				return;
			}

			PasswordEntry.Text = string.Empty;
			ClearStatus();
			await sessionState.SignInAsync(result.User);
		});
	}

	private async void HandleCreateListClicked(object? sender, EventArgs e)
	{
		var details = await PromptForListDetailsAsync(localizer["Shopping.AddNewList"], string.Empty, string.Empty);
		if (!details.Confirmed)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			var createdList = await shoppingListService.CreateListAsync(details.Name, details.Description);
			currentListId = createdList.Id;
			await LoadCurrentListAsync(createdList.Id);
		});
	}

	private async void HandleOpenListClicked(object? sender, EventArgs e)
	{
		if (GetBoundItem<ShoppingList>(sender) is not { } list)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			currentListId = list.Id;
			await LoadCurrentListAsync(list.Id);
		});
	}

	private async void HandleEditListClicked(object? sender, EventArgs e)
	{
		if (GetBoundItem<ShoppingList>(sender) is not { } list)
		{
			return;
		}

		await EditListAsync(list);
	}

	private async void HandleEditCurrentListClicked(object? sender, EventArgs e)
	{
		if (currentList is null)
		{
			return;
		}

		await EditListAsync(currentList);
	}

	private async void HandleArchiveListClicked(object? sender, EventArgs e)
	{
		if (GetBoundItem<ShoppingList>(sender) is not { } list)
		{
			return;
		}

		if (!await DisplayAlert(localizer["Common.Archive"], list.Name, localizer["Common.Archive"], localizer["Common.Cancel"]))
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.ArchiveListAsync(list.Id);
			await LoadOverviewAsync();
		});
	}

	private async void HandleArchiveCurrentListClicked(object? sender, EventArgs e)
	{
		if (currentList is null)
		{
			return;
		}

		if (!await DisplayAlert(localizer["Common.Archive"], currentList.Name, localizer["Common.Archive"], localizer["Common.Cancel"]))
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.ArchiveListAsync(currentList.Id);
			currentListId = null;
			currentList = null;
			await LoadOverviewAsync();
		});
	}

	private async void HandleUnarchiveListClicked(object? sender, EventArgs e)
	{
		if (GetBoundItem<ShoppingList>(sender) is not { } list)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.UnarchiveListAsync(list.Id);
			await LoadOverviewAsync();
		});
	}

	private async void HandleRemoveArchivedListClicked(object? sender, EventArgs e)
	{
		if (GetBoundItem<ShoppingList>(sender) is not { } list)
		{
			return;
		}

		if (!await DisplayAlert(localizer["Common.Remove"], list.Name, localizer["Common.Remove"], localizer["Common.Cancel"]))
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.RemoveArchivedListAsync(list.Id);
			await LoadOverviewAsync();
		});
	}

	private async void HandleBackClicked(object? sender, EventArgs e)
	{
		await RunBusyActionAsync(async () =>
		{
			currentListId = null;
			currentList = null;
			await LoadOverviewAsync();
		});
	}

	private async void HandleAddItemClicked(object? sender, EventArgs e)
	{
		if (currentList is null)
		{
			return;
		}

		var itemName = await PromptForRequiredTextAsync(
			localizer["Shopping.NewItem"],
			localizer["Shopping.AddGroceryItemPlaceholder"],
			localizer["Shopping.Error.ItemNameRequired"]);
		if (itemName is null)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.AddItemAsync(currentList.Id, itemName);
			await LoadCurrentListAsync(currentList.Id);
		});
	}

	private async void HandleItemCheckedChanged(object? sender, CheckedChangedEventArgs e)
	{
		if (currentList is null || GetBoundItem<ShoppingListItem>(sender) is not { } item || item.IsDone == e.Value)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.ToggleDoneAsync(currentList.Id, item.Id, e.Value);
			await LoadCurrentListAsync(currentList.Id);
		});
	}

	private async void HandleRenameItemClicked(object? sender, EventArgs e)
	{
		if (currentList is null || GetBoundItem<ShoppingListItem>(sender) is not { } item)
		{
			return;
		}

		var itemName = await PromptForRequiredTextAsync(
			localizer["Common.Rename"],
			localizer["Shopping.RenameItemPlaceholder"],
			localizer["Shopping.Error.ItemNameRequired"],
			item.Name);
		if (itemName is null)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.RenameItemAsync(currentList.Id, item.Id, itemName);
			await LoadCurrentListAsync(currentList.Id);
		});
	}

	private async void HandleRemoveItemClicked(object? sender, EventArgs e)
	{
		if (currentList is null || GetBoundItem<ShoppingListItem>(sender) is not { } item)
		{
			return;
		}

		if (!await DisplayAlert(localizer["Common.Remove"], item.Name, localizer["Common.Remove"], localizer["Common.Cancel"]))
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.RemoveItemAsync(currentList.Id, item.Id);
			await LoadCurrentListAsync(currentList.Id);
		});
	}

	private async void HandleMoveItemUpClicked(object? sender, EventArgs e)
	{
		await MoveItemAsync(sender, moveUp: true);
	}

	private async void HandleMoveItemDownClicked(object? sender, EventArgs e)
	{
		await MoveItemAsync(sender, moveUp: false);
	}

	private Task HandleSessionChangedAsync(LocalUser? user)
	{
		return MainThread.InvokeOnMainThreadAsync(async () =>
		{
			UpdateChrome();
			if (!initialized)
			{
				return;
			}

			if (user is null)
			{
				currentList = null;
				currentListId = null;
				PasswordEntry.Text = string.Empty;
				ShowLoginView();
				return;
			}

			await RunBusyActionAsync(async () =>
			{
				currentList = null;
				currentListId = null;
				await LoadOverviewAsync();
			});
		});
	}

	private Task HandleStoreChangedAsync(StoreChange change)
	{
		if (!initialized || sessionState.CurrentUser is null || LoadingOverlay.IsVisible)
		{
			return Task.CompletedTask;
		}

		return MainThread.InvokeOnMainThreadAsync(async () =>
		{
			if (sessionState.CurrentUser is null)
			{
				return;
			}

			await RunBusyActionAsync(async () =>
			{
				if (currentListId.HasValue)
				{
					if (change.ListId is null || change.ListId == currentListId.Value)
					{
						await LoadCurrentListAsync(currentListId.Value);
						return;
					}
				}

				await LoadOverviewAsync();
			});
		});
	}

	private async Task EditListAsync(ShoppingList list)
	{
		var details = await PromptForListDetailsAsync(localizer["Common.Edit"], list.Name, list.Description);
		if (!details.Confirmed)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.UpdateListDetailsAsync(list.Id, details.Name, details.Description);
			if (currentListId == list.Id)
			{
				await LoadCurrentListAsync(list.Id);
				return;
			}

			await LoadOverviewAsync();
		});
	}

	private async Task MoveItemAsync(object? sender, bool moveUp)
	{
		if (currentList is null || GetBoundItem<ShoppingListItem>(sender) is not { } item)
		{
			return;
		}

		var orderedItems = OrderItems(currentList).ToList();
		var currentIndex = orderedItems.FindIndex(currentItem => currentItem.Id == item.Id);
		if (currentIndex < 0)
		{
			return;
		}

		var targetIndex = moveUp ? currentIndex - 1 : currentIndex + 1;
		if (targetIndex < 0 || targetIndex >= orderedItems.Count)
		{
			return;
		}

		var targetItem = orderedItems[targetIndex];
		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.ReorderItemAsync(currentList.Id, item.Id, targetItem.Id);
			await LoadCurrentListAsync(currentList.Id);
		});
	}

	private async Task RunBusyActionAsync(Func<Task> action)
	{
		await interactionLock.WaitAsync();
		SetBusy(true);
		try
		{
			await action();
		}
		catch
		{
			ShowStatus(localizer["Account.Error.RequestFailed"], isError: true);
			if (sessionState.CurrentUser is null)
			{
				currentList = null;
				currentListId = null;
				ShowLoginView();
			}
		}
		finally
		{
			SetBusy(false);
			interactionLock.Release();
		}
	}

	private async Task RefreshForCurrentStateAsync()
	{
		if (sessionState.CurrentUser is null)
		{
			currentList = null;
			currentListId = null;
			ShowLoginView();
			return;
		}

		if (currentListId.HasValue)
		{
			await LoadCurrentListAsync(currentListId.Value);
			return;
		}

		await LoadOverviewAsync();
	}

	private async Task LoadOverviewAsync()
	{
		var lists = await shoppingListService.GetListsAsync();
		var activeLists = lists.Where(list => !list.Archived).ToList();
		var archivedLists = lists.Where(list => list.Archived).ToList();

		BindableLayout.SetItemsSource(ActiveListsHost, activeLists);
		ActiveEmptyLabel.IsVisible = activeLists.Count == 0;

		BindableLayout.SetItemsSource(ArchivedListsHost, archivedLists);
		ArchivedHeadingLabel.IsVisible = archivedLists.Count > 0;
		ArchivedEmptyLabel.IsVisible = archivedLists.Count == 0;

		ClearStatus();
		ShowOverviewView();
	}

	private async Task LoadCurrentListAsync(int listId)
	{
		var list = await shoppingListService.GetListAsync(listId);
		if (list is null || list.Archived)
		{
			currentList = null;
			currentListId = null;
			ShowStatus(localizer["Shopping.ListNotFoundDescription"], isError: true);
			await LoadOverviewAsync();
			return;
		}

		currentList = list;
		currentListId = list.Id;

		DetailHeadingLabel.Text = list.Name;
		DetailDescriptionLabel.Text = list.Description;
		DetailSummaryLabel.Text = string.Format(localizer["Shopping.ListSummary"], list.Items.Count(item => item.IsDone), list.Items.Count);
		BindableLayout.SetItemsSource(CurrentItemsHost, OrderItems(list).ToList());
		CurrentItemsEmptyLabel.IsVisible = list.Items.Count == 0;

		ClearStatus();
		ShowDetailView();
	}

	private void ShowLoginView()
	{
		LoginView.IsVisible = true;
		OverviewView.IsVisible = false;
		DetailView.IsVisible = false;
		HeaderContextLabel.Text = localizer["Account.SignInPageTitle"];
	}

	private void ShowOverviewView()
	{
		LoginView.IsVisible = false;
		OverviewView.IsVisible = true;
		DetailView.IsVisible = false;
		HeaderContextLabel.Text = localizer["Shopping.HomeEyebrow"];
		UpdateChrome();
	}

	private void ShowDetailView()
	{
		LoginView.IsVisible = false;
		OverviewView.IsVisible = false;
		DetailView.IsVisible = true;
		HeaderContextLabel.Text = currentList?.Name ?? localizer["Shopping.HomeEyebrow"];
		UpdateChrome();
	}

	private void ConfigureStaticText()
	{
		LoginEyebrowLabel.Text = localizer["Account.AccessEyebrow"];
		LoginTitleLabel.Text = localizer["Account.DefaultTitle"];
		LoginDescriptionLabel.Text = localizer["Account.SignInDescription"];
		UsernameLabel.Text = localizer["Account.Username"];
		PasswordLabel.Text = localizer["Account.Password"];
		UsernameEntry.Placeholder = localizer["Account.Username"];
		PasswordEntry.Placeholder = localizer["Account.Password"];
		LoginButton.Text = localizer["Account.SignIn"];

		OverviewEyebrowLabel.Text = localizer["Shopping.HomeEyebrow"];
		OverviewHeadingLabel.Text = localizer["Shopping.HomeHeading"];
		OverviewDescriptionLabel.Text = localizer["Shopping.HomeDescription"];
		CreateListButton.Text = localizer["Shopping.AddNewList"];
		ActiveEmptyLabel.Text = localizer["Shopping.EmptyState"];
		ArchivedHeadingLabel.Text = localizer["Common.Archived"];
		ArchivedEmptyLabel.Text = localizer["Shopping.EmptyState"];

		BackButton.Text = localizer["Shopping.BackToOverview"];
		EditCurrentListButton.Text = localizer["Common.Edit"];
		ArchiveCurrentListButton.Text = localizer["Common.Archive"];
		AddItemButton.Text = localizer["Shopping.NewItem"];
		ItemHintLabel.Text = localizer["Shopping.ItemInteractionHint"];
		CurrentItemsEmptyLabel.Text = localizer["Shopping.EmptyState"];
		SignOutButton.Text = localizer["Account.SignOut"];
	}

	private void UpdateChrome()
	{
		SignOutButton.IsVisible = sessionState.CurrentUser is not null;
	}

	private void SetBusy(bool isBusy)
	{
		LoadingOverlay.IsVisible = isBusy;
		LoginButton.IsEnabled = !isBusy;
		CreateListButton.IsEnabled = !isBusy;
		BackButton.IsEnabled = !isBusy;
		EditCurrentListButton.IsEnabled = !isBusy;
		ArchiveCurrentListButton.IsEnabled = !isBusy;
		AddItemButton.IsEnabled = !isBusy;
		SignOutButton.IsEnabled = !isBusy;
	}

	private void ShowStatus(string message, bool isError)
	{
		StatusBanner.BackgroundColor = isError ? Color.FromArgb("#2A1117") : Color.FromArgb("#102A22");
		StatusBanner.Stroke = isError ? Color.FromArgb("#7F1D1D") : Color.FromArgb("#1E4F42");
		StatusLabel.TextColor = isError ? Color.FromArgb("#FECACA") : Color.FromArgb("#D1FAE5");
		StatusLabel.Text = message;
		StatusBanner.IsVisible = true;
	}

	private void ClearStatus()
	{
		StatusLabel.Text = string.Empty;
		StatusBanner.IsVisible = false;
	}

	private void ApplyAccessibilityLabels()
	{
		AutomationProperties.SetName(LogoImage, "Boodschap");
		AutomationProperties.SetName(ShoppingListsButton, localizer["Shopping.HomeEyebrow"]);
		AutomationProperties.SetName(SignOutButton, localizer["Account.SignOut"]);
	}

	private string GetAuthenticationErrorMessage(string? errorCode)
	{
		return errorCode switch
		{
			LocalAuthenticationErrorCodes.InvalidCredentials => localizer["Account.Error.InvalidCredentials"],
			LocalAuthenticationErrorCodes.PasswordRequired => localizer["Account.Error.PasswordRequired"],
			LocalAuthenticationErrorCodes.UsernameRequired => localizer["Account.Error.UsernameRequired"],
			_ => localizer["Account.Error.RequestFailed"]
		};
	}

	private async Task<(bool Confirmed, string Name, string Description)> PromptForListDetailsAsync(string title, string initialName, string initialDescription)
	{
		var name = await PromptForRequiredTextAsync(title, localizer["Shopping.TitlePlaceholder"], localizer["Shopping.Error.ListNameRequired"], initialName);
		if (name is null)
		{
			return (false, string.Empty, string.Empty);
		}

		var description = await DisplayPromptAsync(
			title,
			localizer["Shopping.DescriptionPlaceholder"],
			accept: localizer["Common.Save"],
			cancel: localizer["Common.Cancel"],
			placeholder: localizer["Shopping.DescriptionPlaceholder"],
			initialValue: initialDescription);
		if (description is null)
		{
			return (false, string.Empty, string.Empty);
		}

		return (true, name, description.Trim());
	}

	private async Task<string?> PromptForRequiredTextAsync(string title, string message, string emptyValueError, string? initialValue = null)
	{
		var value = await DisplayPromptAsync(
			title,
			message,
			accept: localizer["Common.Save"],
			cancel: localizer["Common.Cancel"],
			placeholder: message,
			initialValue: initialValue ?? string.Empty);
		if (value is null)
		{
			return null;
		}

		var trimmedValue = value.Trim();
		if (trimmedValue.Length == 0)
		{
			ShowStatus(emptyValueError, isError: true);
			return null;
		}

		return trimmedValue;
	}

	private static IEnumerable<ShoppingListItem> OrderItems(ShoppingList list)
	{
		return list.Items
			.OrderBy(item => item.IsDone)
			.ThenBy(item => item.SortOrder)
			.ThenBy(item => item.Id);
	}

	private static T? GetBoundItem<T>(object? sender) where T : class
	{
		return (sender as BindableObject)?.BindingContext as T;
	}

	private sealed class SystemBarInsetsListener(Grid topBar) : Java.Lang.Object, AndroidView.IOnApplyWindowInsetsListener
	{
		private readonly Grid topBar = topBar;
		private Thickness initialPadding;
		private bool initialPaddingCaptured;

		public AndroidWindowInsets OnApplyWindowInsets(AndroidView view, AndroidWindowInsets insets)
		{
			if (!initialPaddingCaptured)
			{
				initialPadding = topBar.Padding;
				initialPaddingCaptured = true;
			}

#pragma warning disable CS0618
			var topInset = insets.GetInsets(
				Android.Views.WindowInsets.Type.StatusBars() | Android.Views.WindowInsets.Type.DisplayCutout()).Top;
			topBar.Padding = new Thickness(
				initialPadding.Left,
				initialPadding.Top + topInset,
				initialPadding.Right,
				initialPadding.Bottom);
#pragma warning restore CS0618
			return insets;
		}
	}
}
