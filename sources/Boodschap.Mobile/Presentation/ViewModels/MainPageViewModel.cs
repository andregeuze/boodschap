using System.Windows.Input;
using System.Globalization;
using Boodschap.Mobile.Presentation.Services;
using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Features.Updates.Application.Contracts;
using Boodschap.Shared.Localization;
using Boodschap.Shared.Realtime;
using Microsoft.Extensions.Localization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace Boodschap.Mobile.Presentation.ViewModels;

public sealed class MainPageViewModel : ObservableObject
{
	private static readonly Color ErrorBackgroundColor = Color.FromArgb("#2A1117");
	private static readonly Color ErrorBorderColor = Color.FromArgb("#7F1D1D");
	private static readonly Color ErrorTextColor = Color.FromArgb("#FECACA");
	private static readonly Color InfoBackgroundColor = Color.FromArgb("#102A22");
	private static readonly Color InfoBorderColor = Color.FromArgb("#1E4F42");
	private static readonly Color InfoTextColor = Color.FromArgb("#D1FAE5");

	private readonly AppInitializationService appInitializationService;
	private readonly ILocalAuthenticationService authenticationService;
	private readonly IShoppingListService shoppingListService;
	private readonly MobileSessionState sessionState;
	private readonly StoreChangeNotifier storeChangeNotifier;
	private readonly IStringLocalizer<AppStrings> localizer;
	private readonly IMobileDialogService dialogService;
	private readonly SemaphoreSlim initializationLock = new(1, 1);
	private readonly SemaphoreSlim interactionLock = new(1, 1);
	private readonly object storeRefreshLock = new();
	private readonly HashSet<int> pendingStoreChangeListIds = [];
	private bool initialized;
	private bool isBusy;
	private bool isStoreRefreshRunning;
	private bool isStoreRefreshAllPending;
	private bool isOverflowMenuOpen;
	private string headerContext;
	private string statusMessage = string.Empty;
	private Color statusBackgroundColor = InfoBackgroundColor;
	private Color statusBorderColor = InfoBorderColor;
	private Color statusTextColor = InfoTextColor;
	private ShoppingList? currentList;
	private int? currentListId;
	private readonly Stack<NavigationEntry> navigationHistory = new();
	private MobileView currentView = MobileView.Login;

	public MainPageViewModel(
		AppInitializationService appInitializationService,
		ILocalAuthenticationService authenticationService,
		IShoppingListService shoppingListService,
		MobileSessionState sessionState,
		StoreChangeNotifier storeChangeNotifier,
		IStringLocalizer<AppStrings> localizer,
		IMobileDialogService dialogService,
		IUpdateCheckService updateCheckService)
	{
		this.appInitializationService = appInitializationService;
		this.authenticationService = authenticationService;
		this.shoppingListService = shoppingListService;
		this.sessionState = sessionState;
		this.storeChangeNotifier = storeChangeNotifier;
		this.localizer = localizer;
		this.dialogService = dialogService;

		headerContext = localizer["Account.SignInPageTitle"].Value;

		Login = new LoginViewModel(localizer, HandleLoginAsync);
		Overview = new ShoppingOverviewViewModel(
			localizer,
			CreateListAsync,
			OpenListAsync,
			EditOverviewListAsync,
			ArchiveOverviewListAsync,
			UnarchiveListAsync,
			RemoveArchivedListAsync);
		Detail = new ShoppingListDetailViewModel(
			localizer,
			NavigateBackAsync,
			EditCurrentListAsync,
			ArchiveCurrentListAsync,
			AddItemAsync,
			ToggleDoneAsync,
			RenameItemAsync,
			RemoveItemAsync,
			ReorderItemAsync);
		Account = new AccountViewModel(localizer, ChangePasswordAsync, CreateUserAsync, updateCheckService);

		HomeCommand = new Command(async () =>
		{
			CloseOverflowMenu();
			await NavigateHomeAsync();
		});
		AccountCommand = new Command(async () =>
		{
			CloseOverflowMenu();
			await ShowAccountAsync();
		});
		SignOutCommand = new Command(async () =>
		{
			CloseOverflowMenu();
			await HandleSignOutAsync();
		});
		ToggleOverflowMenuCommand = new Command(ToggleOverflowMenu);
		CloseOverflowMenuCommand = new Command(CloseOverflowMenu);

		ShowLoginView();
		sessionState.Changed += HandleSessionChangedAsync;
		storeChangeNotifier.Changed += HandleStoreChangedAsync;
	}

	public LoginViewModel Login { get; }

	public ShoppingOverviewViewModel Overview { get; }

	public ShoppingListDetailViewModel Detail { get; }

	public AccountViewModel Account { get; }

	public ICommand HomeCommand { get; }

	public ICommand AccountCommand { get; }

	public ICommand SignOutCommand { get; }

	public ICommand ToggleOverflowMenuCommand { get; }

	public ICommand CloseOverflowMenuCommand { get; }

	public bool IsBusy
	{
		get => isBusy;
		private set
		{
			if (SetProperty(ref isBusy, value))
			{
				OnPropertyChanged(nameof(CanSignOut));
			}
		}
	}

	public bool IsSignedIn => sessionState.CurrentUser is not null;

	public bool CanSignOut => IsSignedIn && !IsBusy;

	public bool IsOverflowMenuOpen
	{
		get => isOverflowMenuOpen;
		private set => SetProperty(ref isOverflowMenuOpen, value);
	}

	public string SignOutText => localizer["Account.SignOut"].Value;

	public string AccountAccessibilityName => localizer["Account.AccountLink"].Value;

	public string ShoppingListsAccessibilityName => localizer["Shopping.HomeEyebrow"].Value;

	public string ShoppingTabText => localizer["Mobile.Navigation.Shopping"].Value;

	public string RecipesTabText => localizer["Recipes.FeatureName"].Value;

	public string NutritionTabText => localizer["Nutrition.FeatureName"].Value;

	public string UpdatesTabText => localizer["Mobile.Navigation.Updates"].Value;

	public string MoreOptionsAccessibilityName => localizer["Mobile.Navigation.MoreOptions"].Value;

	public string HeaderContext
	{
		get => headerContext;
		private set => SetProperty(ref headerContext, value);
	}

	public bool ShouldHandleSystemBack => IsBusy || IsOverflowMenuOpen || navigationHistory.Count > 0 || currentView is MobileView.Detail or MobileView.Account;

	private void ToggleOverflowMenu()
	{
		if (IsSignedIn && !IsBusy)
		{
			IsOverflowMenuOpen = !IsOverflowMenuOpen;
		}
	}

	private void CloseOverflowMenu()
	{
		IsOverflowMenuOpen = false;
	}

	private async Task ShowAccountAsync()
	{
		if (sessionState.CurrentUser is { } user && !Account.IsVisible)
		{
			PushCurrentView();
			ClearStatus();
			Account.SetUser(user);
			ShowAccountView();
			await Account.RefreshUpdateStatusAsync();
		}
	}

	public string StatusMessage
	{
		get => statusMessage;
		private set
		{
			if (SetProperty(ref statusMessage, value))
			{
				OnPropertyChanged(nameof(IsStatusVisible));
			}
		}
	}

	public bool IsStatusVisible => !string.IsNullOrWhiteSpace(StatusMessage);

	public Color StatusBackgroundColor
	{
		get => statusBackgroundColor;
		private set => SetProperty(ref statusBackgroundColor, value);
	}

	public Color StatusBorderColor
	{
		get => statusBorderColor;
		private set => SetProperty(ref statusBorderColor, value);
	}

	public Color StatusTextColor
	{
		get => statusTextColor;
		private set => SetProperty(ref statusTextColor, value);
	}

	public async Task EnsureInitializedAsync()
	{
		if (initialized)
		{
			return;
		}

		await initializationLock.WaitAsync();
		try
		{
			if (initialized)
			{
				return;
			}

			IsBusy = true;
			try
			{
				await appInitializationService.InitializeAsync();
				OnPropertyChanged(nameof(IsSignedIn));
				OnPropertyChanged(nameof(CanSignOut));
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
				IsBusy = false;
			}
		}
		finally
		{
			initializationLock.Release();
		}
	}

	private Task HandleSessionChangedAsync(LocalUser? user)
	{
		return MainThread.InvokeOnMainThreadAsync(() =>
		{
			navigationHistory.Clear();
			CloseOverflowMenu();
			OnPropertyChanged(nameof(IsSignedIn));
			OnPropertyChanged(nameof(CanSignOut));

			if (!initialized)
			{
				if (user is null)
				{
					Login.ClearPassword();
					ShowLoginView();
				}

				return;
			}

			if (user is null)
			{
				currentList = null;
				currentListId = null;
				Login.ClearPassword();
				ClearStatus();
				ShowLoginView();
				return;
			}

			currentList = null;
			currentListId = null;
			_ = RunNavigationActionAsync(LoadOverviewAsync);
		});
	}

	private Task HandleStoreChangedAsync(StoreChange change)
	{
		if (!initialized || sessionState.CurrentUser is null)
		{
			return Task.CompletedTask;
		}

		lock (storeRefreshLock)
		{
			if (change.ListId is int listId)
			{
				pendingStoreChangeListIds.Add(listId);
			}
			else
			{
				isStoreRefreshAllPending = true;
			}

			if (isStoreRefreshRunning)
			{
				return Task.CompletedTask;
			}

			isStoreRefreshRunning = true;
		}

		return MainThread.InvokeOnMainThreadAsync(ProcessStoreRefreshesAsync);
	}

	private async Task ProcessStoreRefreshesAsync()
	{
		while (TryTakePendingStoreChanges(out var refreshAll, out var changedListIds))
		{
			await interactionLock.WaitAsync();
			try
			{
				if (!initialized || sessionState.CurrentUser is null)
				{
					continue;
				}

				if (Overview.IsVisible)
				{
					await LoadOverviewAsync();
				}
				else if (Detail.IsVisible && currentListId is int listId &&
					(refreshAll || changedListIds.Contains(listId)))
				{
					await LoadCurrentListAsync(listId);
				}
			}
			catch
			{
				ShowStatus(localizer["Account.Error.RequestFailed"], isError: true);
			}
			finally
			{
				interactionLock.Release();
			}
		}
	}

	private bool TryTakePendingStoreChanges(out bool refreshAll, out HashSet<int> changedListIds)
	{
		lock (storeRefreshLock)
		{
			if (!isStoreRefreshAllPending && pendingStoreChangeListIds.Count == 0)
			{
				isStoreRefreshRunning = false;
				refreshAll = false;
				changedListIds = [];
				return false;
			}

			refreshAll = isStoreRefreshAllPending;
			changedListIds = [.. pendingStoreChangeListIds];
			isStoreRefreshAllPending = false;
			pendingStoreChangeListIds.Clear();
			return true;
		}
	}

	private async Task HandleLoginAsync(string username, string password)
	{
		await RunBusyActionAsync(async () =>
		{
			var result = await authenticationService.LoginAsync(username, password);
			if (!result.Succeeded || result.User is null)
			{
				ShowStatus(GetAuthenticationErrorMessage(result.ErrorCode), isError: true);
				return;
			}

			Login.ClearPassword();
			ClearStatus();
			await sessionState.SignInAsync(result.User);
		});
	}

	private async Task HandleSignOutAsync()
	{
		await RunBusyActionAsync(async () =>
		{
			ClearStatus();
			await sessionState.SignOutAsync();
		});
	}

	private async Task<LocalPasswordChangeResult> ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
	{
		var result = LocalPasswordChangeResult.Failure(LocalAuthenticationErrorCodes.InvalidCredentials);
		await RunBusyActionAsync(async () =>
		{
			result = await authenticationService.ChangePasswordAsync(
				sessionState.CurrentUser!.Id,
				currentPassword,
				newPassword,
				confirmPassword);
		});
		return result;
	}

	private async Task<LocalAuthenticationResult> CreateUserAsync(string username, string password, string confirmPassword, bool isAdmin)
	{
		var result = LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.InvalidCredentials);
		await RunBusyActionAsync(async () =>
		{
			result = await authenticationService.CreateUserAsync(
				sessionState.CurrentUser!.Id,
				username,
				password,
				confirmPassword,
				isAdmin);
		});
		return result;
	}

	private async Task NavigateHomeAsync()
	{
		if (sessionState.CurrentUser is null || Overview.IsVisible)
		{
			return;
		}

		await RunNavigationActionAsync(async () =>
		{
			PushCurrentView();
			currentList = null;
			currentListId = null;
			await LoadOverviewAsync();
		});
	}

	public async Task NavigateBackAsync()
	{
		if (IsOverflowMenuOpen)
		{
			CloseOverflowMenu();
			return;
		}

		if (IsBusy)
		{
			return;
		}

		await RunNavigationActionAsync(async () =>
		{
			if (navigationHistory.TryPop(out var destination))
			{
				await RestoreNavigationEntryAsync(destination);
				return;
			}

			if (currentView is not (MobileView.Detail or MobileView.Account))
			{
				return;
			}

			currentList = null;
			currentListId = null;
			await LoadOverviewAsync();
		});
	}

	private async Task RestoreNavigationEntryAsync(NavigationEntry destination)
	{
		switch (destination.View)
		{
			case MobileView.Detail when destination.ListId.HasValue:
				currentListId = destination.ListId.Value;
				await LoadCurrentListAsync(destination.ListId.Value);
				break;
			case MobileView.Account when sessionState.CurrentUser is { } user:
				currentList = null;
				currentListId = destination.ListId;
				ClearStatus();
				Account.SetUser(user);
				ShowAccountView();
				await Account.RefreshUpdateStatusAsync();
				break;
			case MobileView.Login:
				currentList = null;
				currentListId = null;
				ShowLoginView();
				break;
			default:
				currentList = null;
				currentListId = null;
				await LoadOverviewAsync();
				break;
		}
	}

	private void PushCurrentView()
	{
		navigationHistory.Push(new NavigationEntry(currentView, currentListId));
	}

	private async Task CreateListAsync()
	{
		var details = await PromptForListDetailsAsync(localizer["Shopping.AddNewList"], string.Empty, string.Empty);
		if (!details.Confirmed)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			PushCurrentView();
			var createdList = await shoppingListService.CreateListAsync(details.Name, details.Description);
			currentListId = createdList.Id;
			await LoadCurrentListAsync(createdList.Id);
		});
	}

	private async Task OpenListAsync(ShoppingListCardViewModel? listCard)
	{
		if (listCard is null)
		{
			return;
		}

		await RunNavigationActionAsync(async () =>
		{
			PushCurrentView();
			currentListId = listCard.List.Id;
			await LoadCurrentListAsync(listCard.List.Id);
		});
	}

	private Task EditOverviewListAsync(ShoppingListCardViewModel? listCard)
	{
		return EditListAsync(listCard?.List);
	}

	private Task EditCurrentListAsync()
	{
		return EditListAsync(currentList);
	}

	private async Task ArchiveOverviewListAsync(ShoppingListCardViewModel? listCard)
	{
		if (listCard is null)
		{
			return;
		}

		if (!await dialogService.ConfirmAsync(localizer["Common.Archive"], listCard.Name, localizer["Common.Archive"], localizer["Common.Cancel"]))
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.ArchiveListAsync(listCard.List.Id);
			await LoadOverviewAsync();
		});
	}

	private async Task ArchiveCurrentListAsync()
	{
		if (currentList is null)
		{
			return;
		}

		if (!await dialogService.ConfirmAsync(localizer["Common.Archive"], currentList.Name, localizer["Common.Archive"], localizer["Common.Cancel"]))
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.ArchiveListAsync(currentList.Id);
			currentList = null;
			currentListId = null;
			await LoadOverviewAsync();
		});
	}

	private async Task UnarchiveListAsync(ShoppingListCardViewModel? listCard)
	{
		if (listCard is null)
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.UnarchiveListAsync(listCard.List.Id);
			await LoadOverviewAsync();
		});
	}

	private async Task RemoveArchivedListAsync(ShoppingListCardViewModel? listCard)
	{
		if (listCard is null)
		{
			return;
		}

		if (!await dialogService.ConfirmAsync(localizer["Common.Remove"], listCard.Name, localizer["Common.Remove"], localizer["Common.Cancel"]))
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.RemoveArchivedListAsync(listCard.List.Id);
			await LoadOverviewAsync();
		});
	}

	private async Task AddItemAsync()
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

	private async Task ToggleDoneAsync(ShoppingItemViewModel? item, bool isDone)
	{
		if (currentList is null || item is null || item.IsDone == isDone)
		{
			return;
		}

		await RunInteractionActionAsync(async () =>
		{
			var currentItem = Detail.Items.FirstOrDefault(candidate => candidate.Item.Id == item.Item.Id);
			if (currentList is null || currentItem is null || currentItem.IsDone == isDone)
			{
				return;
			}

			var previousIsDone = currentItem.IsDone;
			var previousSortOrder = currentItem.Item.SortOrder;
			ApplyLocalToggle(currentItem, isDone);
			try
			{
				if (await shoppingListService.ToggleDoneAsync(currentList.Id, currentItem.Item.Id, isDone) is null)
				{
					throw new InvalidOperationException("The shopping item could not be updated.");
				}
			}
			catch
			{
				currentItem.Item.SortOrder = previousSortOrder;
				currentItem.SetIsDone(previousIsDone);
				RefreshDetailItems();
				throw;
			}
		}, showBusy: false);
	}

	private async Task RenameItemAsync(ShoppingItemViewModel? item)
	{
		if (currentList is null || item is null)
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
			await shoppingListService.RenameItemAsync(currentList.Id, item.Item.Id, itemName);
			await LoadCurrentListAsync(currentList.Id);
		});
	}

	private async Task RemoveItemAsync(ShoppingItemViewModel? item)
	{
		if (currentList is null || item is null)
		{
			return;
		}

		if (!await dialogService.ConfirmAsync(localizer["Common.Remove"], item.Name, localizer["Common.Remove"], localizer["Common.Cancel"]))
		{
			return;
		}

		await RunBusyActionAsync(async () =>
		{
			await shoppingListService.RemoveItemAsync(currentList.Id, item.Item.Id);
			await LoadCurrentListAsync(currentList.Id);
		});
	}

	private async Task ReorderItemAsync(ShoppingItemViewModel? item, ShoppingItemViewModel? targetItem)
	{
		if (currentList is null || item is null || targetItem is null || item.Item.Id == targetItem.Item.Id)
		{
			return;
		}

		await RunInteractionActionAsync(async () =>
		{
			var currentItem = Detail.Items.FirstOrDefault(candidate => candidate.Item.Id == item.Item.Id);
			var currentTargetItem = Detail.Items.FirstOrDefault(candidate => candidate.Item.Id == targetItem.Item.Id);
			if (currentList is null || currentItem is null || currentTargetItem is null)
			{
				return;
			}

			var previousSortOrders = currentList.Items.ToDictionary(listItem => listItem.Id, listItem => listItem.SortOrder);
			ApplyLocalReorder(currentItem.Item, currentTargetItem.Item);
			try
			{
				if (await shoppingListService.ReorderItemAsync(currentList.Id, currentItem.Item.Id, currentTargetItem.Item.Id) is null)
				{
					throw new InvalidOperationException("The shopping item order could not be updated.");
				}
			}
			catch
			{
				foreach (var listItem in currentList.Items)
				{
					listItem.SortOrder = previousSortOrders[listItem.Id];
				}

				RefreshDetailItems();
				throw;
			}
		}, showBusy: false);
	}

	private void ApplyLocalToggle(ShoppingItemViewModel item, bool isDone)
	{
		if (isDone)
		{
			item.Item.SortOrder = currentList!.Items.Max(listItem => listItem.SortOrder) + 1;
		}

		item.SetIsDone(isDone);
		RefreshDetailItems();
	}

	private void ApplyLocalReorder(ShoppingListItem item, ShoppingListItem targetItem)
	{
		var items = currentList!.Items
			.OrderBy(listItem => listItem.SortOrder)
			.ThenBy(listItem => listItem.Id)
			.ToList();
		var from = items.IndexOf(item);
		var to = items.IndexOf(targetItem);
		items.RemoveAt(from);
		items.Insert(to, item);

		for (var index = 0; index < items.Count; index++)
		{
			items[index].SortOrder = index;
		}

		RefreshDetailItems();
	}

	private void RefreshDetailItems()
	{
		var itemViewModelsById = Detail.Items.ToDictionary(item => item.Item.Id);
		var orderedItemViewModels = OrderItems(currentList!)
			.Select(item => itemViewModelsById[item.Id])
			.ToList();

		for (var index = 0; index < orderedItemViewModels.Count; index++)
		{
			var currentIndex = Detail.Items.IndexOf(orderedItemViewModels[index]);
			if (currentIndex != index)
			{
				Detail.Items.Move(currentIndex, index);
			}
		}

		Detail.Summary = string.Format(
			CultureInfo.CurrentCulture,
			localizer["Shopping.ListSummary"].Value,
			currentList!.Items.Count(item => item.IsDone),
			currentList.Items.Count);
	}

	private async Task EditListAsync(ShoppingList? list)
	{
		if (list is null)
		{
			return;
		}

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

	private async Task RunBusyActionAsync(Func<Task> action)
	{
		await RunInteractionActionAsync(action, showBusy: true);
	}

	private async Task RunNavigationActionAsync(Func<Task> action)
	{
		await RunInteractionActionAsync(action, showBusy: false);
	}

	private async Task RunInteractionActionAsync(Func<Task> action, bool showBusy)
	{
		await interactionLock.WaitAsync();
		if (showBusy)
		{
			IsBusy = true;
		}

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
			if (showBusy)
			{
				IsBusy = false;
			}

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
		Overview.SetLists(lists);
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
			await LoadOverviewAsync();
			ShowStatus(localizer["Shopping.ListNotFoundDescription"], isError: true);
			return;
		}

		currentList = list;
		currentListId = list.Id;

		var orderedItems = OrderItems(list).ToList();
		var itemViewModels = orderedItems.Select(item => new ShoppingItemViewModel(item)).ToList();
		var summaryText = string.Format(
			CultureInfo.CurrentCulture,
			localizer["Shopping.ListSummary"].Value,
			list.Items.Count(item => item.IsDone),
			list.Items.Count);

		Detail.SetList(list, itemViewModels, summaryText);
		ClearStatus();
		ShowDetailView(list.Name);
	}

	private void ShowLoginView()
	{
		currentView = MobileView.Login;
		Login.IsVisible = true;
		Overview.IsVisible = false;
		Detail.IsVisible = false;
		Account.IsVisible = false;
		Account.ClearForms();
		Detail.ClearList();
		HeaderContext = localizer["Account.SignInPageTitle"].Value;
	}

	private void ShowOverviewView()
	{
		currentView = MobileView.Overview;
		Login.IsVisible = false;
		Overview.IsVisible = true;
		Detail.IsVisible = false;
		Account.IsVisible = false;
		Account.ClearForms();
		Detail.ClearList();
		HeaderContext = localizer["Shopping.HomeEyebrow"].Value;
	}

	private void ShowDetailView(string title)
	{
		currentView = MobileView.Detail;
		Login.IsVisible = false;
		Overview.IsVisible = false;
		Detail.IsVisible = true;
		Account.IsVisible = false;
		Account.ClearForms();
		HeaderContext = title;
	}

	private void ShowAccountView()
	{
		currentView = MobileView.Account;
		Login.IsVisible = false;
		Overview.IsVisible = false;
		Detail.IsVisible = false;
		Detail.ClearList();
		Account.IsVisible = true;
		HeaderContext = localizer["Account.Title"].Value;
	}

	private void ShowStatus(string message, bool isError)
	{
		StatusBackgroundColor = isError ? ErrorBackgroundColor : InfoBackgroundColor;
		StatusBorderColor = isError ? ErrorBorderColor : InfoBorderColor;
		StatusTextColor = isError ? ErrorTextColor : InfoTextColor;
		StatusMessage = message;
	}

	private void ClearStatus()
	{
		StatusMessage = string.Empty;
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

	private enum MobileView
	{
		Login,
		Overview,
		Detail,
		Account
	}

	private readonly record struct NavigationEntry(MobileView View, int? ListId);

	private async Task<(bool Confirmed, string Name, string Description)> PromptForListDetailsAsync(string title, string initialName, string initialDescription)
	{
		var name = await PromptForRequiredTextAsync(title, localizer["Shopping.TitlePlaceholder"], localizer["Shopping.Error.ListNameRequired"], initialName);
		if (name is null)
		{
			return (false, string.Empty, string.Empty);
		}

		var description = await dialogService.PromptAsync(
			title,
			localizer["Shopping.DescriptionPlaceholder"],
			localizer["Common.Save"],
			localizer["Common.Cancel"],
			localizer["Shopping.DescriptionPlaceholder"],
			initialDescription);
		if (description is null)
		{
			return (false, string.Empty, string.Empty);
		}

		return (true, name, description.Trim());
	}

	private async Task<string?> PromptForRequiredTextAsync(string title, string message, string emptyValueError, string? initialValue = null)
	{
		var value = await dialogService.PromptAsync(
			title,
			message,
			localizer["Common.Save"],
			localizer["Common.Cancel"],
			message,
			initialValue ?? string.Empty);
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
			.OrderBy(item => item.SortOrder)
			.ThenBy(item => item.Id);
	}
}