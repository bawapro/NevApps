using Microsoft.AspNetCore.Components.WebView;

namespace NevApps
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
        }

        private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
        {
#if ANDROID
            // OxygenOS / Android font scale enlarges rem-based MudBlazor icons and wraps the top nav.
            e.WebView.Settings.TextZoom = 100;
#endif
        }
    }
}
