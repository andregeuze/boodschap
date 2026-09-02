using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Boodschap.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

#if DEBUG
		Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
		Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
#endif

		Window?.SetDecorFitsSystemWindows(true);
		Window?.SetStatusBarColor(Android.Graphics.Color.Rgb(7, 7, 8));
		SetDarkStatusBarIcons();
	}

	protected override void OnResume()
	{
		base.OnResume();
		SetDarkStatusBarIcons();
	}

	private void SetDarkStatusBarIcons()
	{
		if (Window?.DecorView is { } decorView)
		{
#pragma warning disable CS0618
			decorView.SystemUiVisibility = (StatusBarVisibility)((int)decorView.SystemUiVisibility & ~(int)SystemUiFlags.LightStatusBar);
#pragma warning restore CS0618
		}
	}
}
