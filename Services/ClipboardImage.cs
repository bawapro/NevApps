namespace NevApps.Services;

public static class ClipboardImage
{
    // Clipboard APIs must be called on the UI thread.
    public static Task<bool> HasImageAsync() =>
        MainThread.InvokeOnMainThreadAsync(HasImageCore);

    public static Task<Stream?> OpenImageStreamAsync() =>
        MainThread.InvokeOnMainThreadAsync(OpenImageCoreAsync);

#if ANDROID
    private static global::Android.Content.ClipboardManager? Manager =>
        global::Android.App.Application.Context
            .GetSystemService(global::Android.Content.Context.ClipboardService)
            as global::Android.Content.ClipboardManager;

    private static bool HasImageCore() =>
        Manager?.PrimaryClipDescription?.HasMimeType("image/*") == true;

    private static Task<Stream?> OpenImageCoreAsync()
    {
        var resolver = global::Android.App.Application.Context.ContentResolver;
        var clip = Manager?.PrimaryClip;
        if (resolver == null || clip == null) return Task.FromResult<Stream?>(null);

        for (var i = 0; i < clip.ItemCount; i++)
        {
            var uri = clip.GetItemAt(i)?.Uri;
            if (uri == null) continue;

            if (resolver.GetType(uri)?.StartsWith("image/") == true)
                return Task.FromResult(resolver.OpenInputStream(uri));
        }

        return Task.FromResult<Stream?>(null);
    }

#elif IOS || MACCATALYST
    private static bool HasImageCore() => UIKit.UIPasteboard.General.HasImages;

    private static Task<Stream?> OpenImageCoreAsync()
    {
        var pasteboard = UIKit.UIPasteboard.General;
        foreach (var type in new[] { "public.jpeg", "public.png" })
        {
            using var data = pasteboard.DataForPasteboardType(type);
            if (data == null) continue;
            return Task.FromResult<Stream?>(new MemoryStream(data.ToArray()));
        }

        using var image = pasteboard.Image;
        if (image == null)
            return Task.FromResult<Stream?>(null);

        using var png = image.AsPNG();
        return Task.FromResult<Stream?>(png == null ? null : new MemoryStream(png.ToArray()));
    }

#elif WINDOWS
    private static bool HasImageCore() =>
        global::Windows.ApplicationModel.DataTransfer.Clipboard.GetContent()
            .Contains(global::Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap);

    private static async Task<Stream?> OpenImageCoreAsync()
    {
        var content = global::Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
        if (!content.Contains(global::Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
            return null;

        var reference = await content.GetBitmapAsync();
        using var source = await reference.OpenReadAsync();
        var ms = new MemoryStream();
        await source.AsStreamForRead().CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

#else
    private static bool HasImageCore() => false;
    private static Task<Stream?> OpenImageCoreAsync() => Task.FromResult<Stream?>(null);
#endif
}
