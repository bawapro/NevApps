namespace NevApps.Interfaces;

public interface IStepTrackerService
{
    bool IsTracking { get; }
    int CurrentSteps { get; }
    double TotalDistanceKm { get; }
    char StatusCode { get; }
    string CurrentPlaceName { get; }
    int TotalSeconds { get; }

    void StartTracking(bool isResuming);
    void PauseTracking();
    void StopTracking();

    // Events to push real-time updates directly up to MudBlazor
    event Action<int> OnStepCountChanged;
    event Action<double> OnDistanceChanged;
    event Action<bool> OnTrackingStateChanged;
    event Action<char> OnStatusUpdated; 
    event Action<string>? OnPlaceNameChanged;
    event Action? OnTimerTicked;
}