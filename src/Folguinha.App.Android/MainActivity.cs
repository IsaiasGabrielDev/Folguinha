using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace Folguinha.App.Android;

[Activity(
    Label = "Folguinha.App.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
