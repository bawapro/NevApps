using CoreLocation;
using NevApps.Interfaces;
using Plugin.Maui.Pedometer;

namespace NevApps.Platforms.iOS;

public class StepTrackerService : IStepTrackerService
{
    public bool IsTracking { get; private set; }
    public int CurrentSteps { get; private set; }
    public double TotalDistanceKm { get; private set; }
    public char StatusCode { get; private set; } = 'R';
    public string CurrentPlaceName { get; private set; } = "Locating...";

    private int _elapsedSeconds = 0;
    public int TotalSeconds => _elapsedSeconds;

    private int? _initialStepBaseline;
    private int _stepsAccumulatedBeforePause = 0;
    private DateTime _lastMovementTime = DateTime.UtcNow;
    private DateTime _lastGeocodeTime = DateTime.MinValue;
    private CLLocation? _lastGpsLocation;
    private CLLocationManager? _locationManager;
    private Timer? _backgroundTimer;

    public event Action<int>? OnStepCountChanged;
    public event Action<double>? OnDistanceChanged;
    public event Action<bool>? OnTrackingStateChanged;
    public event Action<char>? OnStatusUpdated;
    public event Action<string>? OnPlaceNameChanged;
    public event Action? OnTimerTicked;

    public void StartTracking(bool isResuming = false)
    {
        if (IsTracking) return;

        if (!isResuming)
        {
            ResetSessionData();
        }
        else
        {
            _stepsAccumulatedBeforePause = CurrentSteps;
            _initialStepBaseline = null;
        }

        _lastMovementTime = DateTime.UtcNow;

        SetupLocationManager();
        SetupPedometer();
        SetupTimer();

        IsTracking = true;
        StatusCode = 'M';
        NotifyUi();
    }

    private void ResetSessionData()
    {
        CurrentSteps = 0;
        TotalDistanceKm = 0;
        _initialStepBaseline = null;
        _stepsAccumulatedBeforePause = 0;
        _lastGpsLocation = null;
        _elapsedSeconds = 0;
    }

    private void SetupLocationManager()
    {
        _locationManager ??= new CLLocationManager
        {
            DesiredAccuracy = CLLocation.AccuracyBestForNavigation,
            DistanceFilter = 5,
            AllowsBackgroundLocationUpdates = true,
            PausesLocationUpdatesAutomatically = false,
            ShowsBackgroundLocationIndicator = true,
            ActivityType = CLActivityType.Fitness
        };

        _locationManager.LocationsUpdated += OnLocationsUpdated;
        _locationManager.StartUpdatingLocation();
    }

    private void SetupPedometer()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Pedometer.Default.ReadingChanged -= OnPedometerReadingChanged;
            Pedometer.Default.ReadingChanged += OnPedometerReadingChanged;
            Pedometer.Default.Start();
        });
    }

    private void SetupTimer()
    {
        _backgroundTimer = new Timer(_ =>
        {
            if (!IsTracking) return;

            // Only increment time when NOT idle
            if (StatusCode != 'I' && StatusCode != 'S' && StatusCode != 'R')
            {
                _elapsedSeconds++;
                OnTimerTicked?.Invoke();
            }

            CheckIdleState();   // Still check for idle/moving transitions
        }, null, 0, 1000);
    }

    public void PauseTracking() => StopHardware(true);
    public void StopTracking() => StopHardware(false);

    private void StopHardware(bool isPause)
    {
        if (!IsTracking) return;

        _locationManager?.StopUpdatingLocation();
        if (_locationManager != null)
            _locationManager.LocationsUpdated -= OnLocationsUpdated;

        Pedometer.Default.ReadingChanged -= OnPedometerReadingChanged;
        Pedometer.Default.Stop();

        _backgroundTimer?.Dispose();
        _backgroundTimer = null;

        IsTracking = false;
        StatusCode = isPause ? 'I' : 'S';
        NotifyUi();
    }

    private void OnPedometerReadingChanged(object? sender, PedometerData e)
    {
        if (_initialStepBaseline == null)
            _initialStepBaseline = e.NumberOfSteps;

        int stepsThisSession = e.NumberOfSteps - _initialStepBaseline.Value;
        CurrentSteps = stepsThisSession + _stepsAccumulatedBeforePause;

        OnStepCountChanged?.Invoke(CurrentSteps);
        _lastMovementTime = DateTime.UtcNow;

        if (StatusCode != 'M')
        {
            StatusCode = 'M';
            OnStatusUpdated?.Invoke(StatusCode);
        }
    }

    private void OnLocationsUpdated(object? sender, CLLocationsUpdatedEventArgs e)
    {
        if (!IsTracking) return;

        var currentGps = e.Locations.LastOrDefault();
        if (currentGps == null || currentGps.HorizontalAccuracy > 15) return;

        // Update movement time if steps increased
        if (CurrentSteps > 0) _lastMovementTime = DateTime.UtcNow;

        CheckIdleState();

        // Distance
        if (StatusCode == 'M' && _lastGpsLocation != null)
        {
            double distanceDelta = _lastGpsLocation.DistanceFrom(currentGps) / 1000.0;
            if (distanceDelta > 0.002 && WasMovingRecently())
            {
                TotalDistanceKm += distanceDelta;
                OnDistanceChanged?.Invoke(TotalDistanceKm);
            }
        }

        _lastGpsLocation = currentGps;

        // Throttled geocoding
        if (DateTime.UtcNow - _lastGeocodeTime > TimeSpan.FromSeconds(25))
        {
            _lastGeocodeTime = DateTime.UtcNow;
            _ = UpdatePlaceNameAsync(new Location(currentGps.Coordinate.Latitude, currentGps.Coordinate.Longitude));
        }
    }

    private bool WasMovingRecently() => (DateTime.UtcNow - _lastMovementTime).TotalSeconds < 15;

    private void CheckIdleState()
    {
        if (DateTime.UtcNow - _lastMovementTime >= TimeSpan.FromMinutes(1))
        {
            if (StatusCode != 'I')
            {
                StatusCode = 'I';
                OnStatusUpdated?.Invoke(StatusCode);
            }
        }
    }

    private async Task UpdatePlaceNameAsync(Location location)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                string name = string.IsNullOrEmpty(placemark.Thoroughfare)
                    ? placemark.Locality ?? "Unknown"
                    : $"{placemark.Thoroughfare}, {placemark.Locality}";

                if (CurrentPlaceName != name)
                {
                    CurrentPlaceName = name;
                    OnPlaceNameChanged?.Invoke(name);
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