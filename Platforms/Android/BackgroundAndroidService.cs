using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Plugin.Maui.Pedometer;
using Android.Locations; 

namespace NevApps;

[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeHealth)]
public class BackgroundAndroidService : Service, ILocationListener
{
    private const string ChannelId = "step_tracker_channel";
    private const int NotificationId = 9001;

    private int? _initialStepBaseline;
    private LocationManager? _locationManager;

    public override IBinder? OnBind(Intent? intent) => null;
    private PowerManager.WakeLock _wakeLock;
    
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("NevApps Tracking Active")
            .SetContentText("Tracking steps and distance in the background...")
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuMyPlaces)
            .SetOngoing(true)
            .Build();

        StartForeground(NotificationId, notification);

        InitializeTracking();
        
        var powerManager = (PowerManager)GetSystemService(PowerService);
        if (powerManager != null && (_wakeLock == null || !_wakeLock.IsHeld))
        {
            // "StepTracker:WakeLockTag" helps you trace it in the device profile logs
            _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "StepTracker:WakeLockTag");
            _wakeLock.Acquire(); // Forces Android CPU to stay awake for your timer loops
        }
        return StartCommandResult.Sticky;
    }

    private void InitializeTracking()
    {
        // 1. Hook up the hardware pedometer readings
        Pedometer.Default.ReadingChanged += OnPedometerReadingChanged;
        Pedometer.Default.Start();

        // 2. Start Native Android Location Hardware Loop
        _locationManager = (LocationManager)GetSystemService(Context.LocationService)!;
        
        try
        {
            // Request updates every 2000ms (2 seconds) OR every 2 meters walked
            _locationManager.RequestLocationUpdates(
                LocationManager.GpsProvider, 
                2000, 
                2f, 
                this);
        }
        catch (Exception)
        {
            /* Handle missing runtime permissions gracefully */
        }
    }

    // NATIVE INTERFACE CALLBACKS: Triggers automatically on changes
   public void OnLocationChanged(Android.Locations.Location location)
    {
        if (location == null) return;

        if (location.HasAccuracy && location.Accuracy > 15)
        {
            return; // Drop the point if accuracy error is wider than 15 meters
        }

        var locationAgeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds() - location.Time;
        if (locationAgeMs > 5000) 
        {
            return; // Drop the point if it's older than 5 seconds
        }

        // Map native Android fields cleanly over to MAUI sensors structure
        var mauiLocation = new Microsoft.Maui.Devices.Sensors.Location
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Altitude = location.HasAltitude ? location.Altitude : null,
            Accuracy = location.HasAccuracy ? location.Accuracy : null,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time)
        };

        // Only stream the coordinate data if it survived both shields!
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StepTrackerService.Instance?.HandleLocationUpdate(mauiLocation);
        });
        
        /* Option 2: to calculate distance instead of location
         private Android.Locations.Location _lastLocation;
           private double _totalDistanceMeters = 0;
           
           public void OnLocationChanged(Android.Locations.Location location)
           {
               // Guard 1: Ignore inaccurate data points (e.g., if accuracy radius is worse than 15 meters)
               if (location.HasAccuracy && location.Accuracy > 15) 
               {
                   return; 
               }
           
               if (_lastLocation != null)
               {
                   // Calculate the straight-line distance between the previous point and current point
                   float distanceToNewPoint = _lastLocation.DistanceTo(location);
           
                   // Guard 2: Prevent GPS Jitter
                   // If you move less than 1 meter or are moving at an impossible speed for walking, ignore it
                   if (distanceToNewPoint > 1.0)
                   {
                       _totalDistanceMeters += distanceToNewPoint;
                       
                       // Convert to kilometers and pass it up to your StepTrackerService
                       double totalKm = _totalDistanceMeters / 1000.0;
                       StepTrackerService.Instance?.UpdateDistanceFromHardware(totalKm);
                   }
               }
           
               _lastLocation = location;
           }
         */
    }

    private void OnPedometerReadingChanged(object? sender, PedometerData e)
    {
        if (_initialStepBaseline == null)
        {
            _initialStepBaseline = e.NumberOfSteps; //Total steps reading since the phone was ON
        }

        int stepsThisSession = e.NumberOfSteps - _initialStepBaseline.Value;
        StepTrackerService.Instance?.UpdateStepsFromHardware(stepsThisSession);
    }

    public override void OnDestroy()
    {
        Pedometer.Default.ReadingChanged -= OnPedometerReadingChanged;
        Pedometer.Default.Stop();

        if (_locationManager != null)
        {
            _locationManager.RemoveUpdates(this);
        }
        if (_wakeLock != null && _wakeLock.IsHeld)
        {
            _wakeLock.Release();
            _wakeLock = null;
        }
        _initialStepBaseline = null;
        base.OnDestroy();
    }

    // Required interface stub definitions for ILocationListener
    public void OnProviderDisabled(string provider) { }
    public void OnProviderEnabled(string provider) { }
    public void OnStatusChanged(string? provider, Availability status, Bundle? extras) { }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Activity Session Tracker", NotificationImportance.Low)
            {
                Description = "Required persistent notification for NevApps background processing."
            };
            var manager = (NotificationManager)GetSystemService(Context.NotificationService)!;
            manager.CreateNotificationChannel(channel);
        }
    }
    
}