using Android.Content;
using Android.OS;
using NevApps.Interfaces;

namespace NevApps;

public class ActivityRecognitionPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[] { (Android.Manifest.Permission.ActivityRecognition, true) };
}

public class StepTrackerService : IStepTrackerService
{
    public bool IsTracking { get; private set; }
    public int CurrentSteps { get; private set; }
    public double TotalDistanceKm { get; private set; }
    public char StatusCode { get; private set; } = 'R';
    public string CurrentPlaceName { get; private set; } = "Locating...";
    private int _elapsedSeconds = 0;
    public int TotalSeconds => _elapsedSeconds;

    public static StepTrackerService? Instance { get; private set; }

    public event Action<int>? OnStepCountChanged;
    public event Action<double>? OnDistanceChanged;
    public event Action<bool>? OnTrackingStateChanged;
    public event Action<char>? OnStatusUpdated;
    public event Action<string>? OnPlaceNameChanged;
    public event Action? OnTimerTicked;

    private Timer? _backgroundTimer;
    private Location? _lastLocation;
    private DateTime _lastMovementTime = DateTime.Now;
    private DateTime _lastGeocodeTime = DateTime.MinValue;
    private int _stepsAccumulatedBeforePause = 0;

    public StepTrackerService()
    {
        Instance = this;
    }

    public void StartTracking(bool isResuming = false)
    {
        if (IsTracking) return;

        if (!isResuming)
            ResetSessionData();
        else
            _stepsAccumulatedBeforePause = CurrentSteps;

        _lastMovementTime = DateTime.Now;

        // Start Android Background Service
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(BackgroundAndroidService));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);

        IsTracking = true;
        StatusCode = 'M';

        SetupTimer();
        NotifyUi();
    }

    private void ResetSessionData()
    {
        CurrentSteps = 0;
        TotalDistanceKm = 0;
        _lastLocation = null;
        _elapsedSeconds = 0;
        _stepsAccumulatedBeforePause = 0;
        CurrentPlaceName = "Locating...";
    }

    private void SetupTimer()
    {
        _backgroundTimer = new Timer(_ =>
        {
            if (!IsTracking) return;

            // Only increment when actively tracking (not idle)
            if (StatusCode != 'I' && StatusCode != 'S' && StatusCode != 'R')
            {
                _elapsedSeconds++;
                OnTimerTicked?.Invoke();
            }

            CheckIdleState();
        }, null, 0, 1000);
    }

    public void PauseTracking() => StopService(true);
    public void StopTracking() => StopService(false);

    private void StopService(bool isPause)
    {
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(BackgroundAndroidService));
        context.StopService(intent);

        _backgroundTimer?.Dispose();
        _backgroundTimer = null;

        IsTracking = false;
        StatusCode = isPause ? 'I' : 'S';
        NotifyUi();
    }

    public void UpdateStepsFromHardware(int hardwareSessionSteps)
    {
        CurrentSteps = hardwareSessionSteps + _stepsAccumulatedBeforePause;
        OnStepCountChanged?.Invoke(CurrentSteps);
        _lastMovementTime = DateTime.Now;

        if (StatusCode != 'M')
        {
            StatusCode = 'M';
            OnStatusUpdated?.Invoke(StatusCode);
        }
    }

    public void HandleLocationUpdate(Location newLocation)
    {
        if (!IsTracking || newLocation.Accuracy > 15) return;

        if (CurrentSteps > 0) 
            _lastMovementTime = DateTime.Now;

        CheckIdleState();

        if (StatusCode == 'M' && _lastLocation != null && WasMovingRecently())
        {
            double delta = Location.CalculateDistance(_lastLocation, newLocation, DistanceUnits.Kilometers);
            if (delta > 0.002)
            {
                TotalDistanceKm += delta;
                OnDistanceChanged?.Invoke(TotalDistanceKm);
            }
        }

        _lastLocation = newLocation;

        // Throttled geocoding
        if ((DateTime.Now - _lastGeocodeTime).TotalSeconds > 25)
        {
            _lastGeocodeTime = DateTime.Now;
            _ = UpdatePlaceNameAsync(newLocation);
        }
    }

    private bool WasMovingRecently() => (DateTime.Now - _lastMovementTime).TotalSeconds < 15;

    private void CheckIdleState()
    {
        if ((DateTime.Now - _lastMovementTime).TotalMinutes >= 1 && StatusCode != 'I')
        {
            StatusCode = 'I';
            OnStatusUpdated?.Invoke(StatusCode);
        }
    }

    private async Task UpdatePlaceNameAsync(Location location)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                string formatted = string.IsNullOrEmpty(placemark.Thoroughfare)
                    ? placemark.Locality ?? "Unknown"
                    : $"{placemark.Thoroughfare}, {placemark.Locality}";

                if (CurrentPlaceName != formatted)
                {
                    CurrentPlaceName = formatted;
                    OnPlaceNameChanged?.Invoke(formatted);
                }
            }
        }
        catch
        {
            CurrentPlaceName = "Location Locked";
        }
    }

    private void NotifyUi()
    {
        OnTrackingStateChanged?.Invoke(IsTracking);
        OnStatusUpdated?.Invoke(StatusCode);
        OnStepCountChanged?.Invoke(CurrentSteps);
        OnDistanceChanged?.Invoke(TotalDistanceKm);
        OnPlaceNameChanged?.Invoke(CurrentPlaceName);
    }
}