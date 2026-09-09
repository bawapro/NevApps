using NevApps.Interfaces;

namespace MobileNevApps.Tests.Helpers;

internal sealed class FakeStepTrackerService : IStepTrackerService
{
    public bool IsTracking { get; private set; }
    public int CurrentSteps { get; set; }
    public double TotalDistanceKm { get; set; }
    public char StatusCode { get; set; } = 'I';
    public string CurrentPlaceName { get; set; } = string.Empty;
    public int TotalSeconds { get; set; }

    public event Action<int> OnStepCountChanged = _ => { };
    public event Action<double> OnDistanceChanged = _ => { };
    public event Action<bool> OnTrackingStateChanged = _ => { };
    public event Action<char> OnStatusUpdated = _ => { };
    public event Action<string>? OnPlaceNameChanged;
    public event Action? OnTimerTicked;

    public void StartTracking(bool isResuming)
    {
        IsTracking = true;
        OnTrackingStateChanged(true);
    }

    public void PauseTracking()
    {
        OnTrackingStateChanged(false);
    }

    public void StopTracking()
    {
        IsTracking = false;
        OnTrackingStateChanged(false);
    }
}
