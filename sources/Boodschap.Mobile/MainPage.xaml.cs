using Boodschap.Mobile.Presentation.ViewModels;
using AndroidView = Android.Views.View;
using AndroidWindowInsets = Android.Views.WindowInsets;

namespace Boodschap.Mobile;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private MainPageViewModel ViewModel => (MainPageViewModel)BindingContext;

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = ViewModel.EnsureInitializedAsync();
	}

	protected override bool OnBackButtonPressed()
	{
		if (!ViewModel.ShouldHandleSystemBack)
		{
			return base.OnBackButtonPressed();
		}

		_ = ViewModel.NavigateBackAsync();
		return true;
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

	private void ApplyAccessibilityLabels()
	{
		AutomationProperties.SetName(LogoImage, "Boodschap");
		AutomationProperties.SetName(ShoppingListsButton, ViewModel.ShoppingListsAccessibilityName);
		AutomationProperties.SetName(RecipesTabButton, ViewModel.RecipesTabText);
		AutomationProperties.SetName(NutritionTabButton, ViewModel.NutritionTabText);
		AutomationProperties.SetName(UpdatesTabButton, ViewModel.UpdatesTabText);
		AutomationProperties.SetName(OverflowMenuButton, ViewModel.MoreOptionsAccessibilityName);
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
