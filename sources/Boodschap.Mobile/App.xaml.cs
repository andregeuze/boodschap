namespace Boodschap.Mobile;

public partial class App : Application
{
	private readonly IServiceProvider services;

	public App(IServiceProvider services)
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Dark;
		this.services = services;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var mainPage = services.GetRequiredService<MainPage>();
		return new Window(mainPage);
	}
}