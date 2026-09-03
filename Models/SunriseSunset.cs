namespace NevApps.Models
{
    public class Results
    {
        public string? sunrise { get; set; }
        public string? sunset { get; set; }
        public string? solar_noon { get; set; }
        public string? day_length { get; set; }
        public string? civil_twilight_begin { get; set; }
        public string? civil_twilight_end { get; set; }
        public string? nautical_twilight_begin { get; set; }
        public string? nautical_twilight_end { get; set; }
        public string? astronomical_twilight_begin { get; set; }
        public string? astronomical_twilight_end { get; set; }
    }

    public class SunriseSunset
    {
        public Results? results { get; set; }
        public string? status { get; set; }
        public string? tzid { get; set; }
    }
}