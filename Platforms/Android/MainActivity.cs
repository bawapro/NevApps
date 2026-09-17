using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Activity;

namespace NevApps
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Shrink the webview with the keypad so autocomplete lists are not
            // drawn under the keyboard (OxygenOS default can overlay instead).
            Window?.SetSoftInputMode(SoftInput.AdjustResize);

            //EdgeToEdge.Enable(this);
        }
    }
}
