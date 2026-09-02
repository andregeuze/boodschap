namespace Boodschap.Mobile;

public partial class App : Application
{
	private readonly MainPage mainPage;

	public App(MainPage mainPage)
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Dark;
		this.mainPage = mainPage;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(mainPage);
	}
}