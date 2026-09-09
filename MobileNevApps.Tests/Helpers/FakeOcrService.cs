using Plugin.Maui.OCR;

namespace MobileNevApps.Tests.Helpers;

internal sealed class FakeOcrService : IOcrService
{
    public event EventHandler<OcrCompletedEventArgs>? RecognitionCompleted;
    public IReadOnlyCollection<string> SupportedLanguages { get; } = Array.Empty<string>();

    public Task InitAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<OcrResult> RecognizeTextAsync(byte[] imageData, bool tryHard = false, CancellationToken ct = default)
        => Task.FromResult(new OcrResult { Success = false, AllText = string.Empty });

    public Task<OcrResult> RecognizeTextAsync(byte[] imageData, OcrOptions options, CancellationToken ct = default)
        => Task.FromResult(new OcrResult { Success = false, AllText = string.Empty });

    public Task StartRecognizeTextAsync(byte[] imageData, OcrOptions options, CancellationToken ct = default)
        => Task.CompletedTask;
}
