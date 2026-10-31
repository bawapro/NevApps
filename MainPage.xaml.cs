using Microsoft.AspNetCore.Components.WebView;

namespace NevApps;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
#if IOS
        // Let the WebView draw into the home-indicator inset so the back
        // strip can fill the curved bottom. Top/sides still avoid the notch.
        SafeAreaEdges = new SafeAreaEdges(
            SafeAreaRegions.Container,
            SafeAreaRegions.Container,
            SafeAreaRegions.Container,
            SafeAreaRegions.None);
#endif
        blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
    }

    private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
#if ANDROID
            // OxygenOS / Android font scale enlarges rem-based MudBlazor icons and wraps the top nav.
            e.WebView.Settings.TextZoom = 100;
#endif
#if IOS
        // Keep the native WebView available for normal iOS touch/keyboard
        // behavior. The detail and section panes own page scrolling in CSS.
        var scrollView = e.WebView.ScrollView;
        scrollView.ScrollEnabled = true;
        scrollView.ContentInsetAdjustmentBehavior =
            UIKit.UIScrollViewContentInsetAdjustmentBehavior.Never;
#endif
    }
}