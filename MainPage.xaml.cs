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
#if IOS
            // WKWebView's UIScrollView pans the whole page when the keyboard opens
            // (edit dialog fields). That leaves empty space above the keypad; further
            // pans add more. Page scrolling stays on CSS overflow inside the document.
            var scrollView = e.WebView.ScrollView;
            scrollView.ScrollEnabled = false;
            scrollView.Bounces = false;
            scrollView.AlwaysBounceVertical = false;
            scrollView.ContentInsetAdjustmentBehavior =
                UIKit.UIScrollViewContentInsetAdjustmentBehavior.Never;
#endif
        }
    }
}
