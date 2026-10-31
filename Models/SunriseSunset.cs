namespace NevApps.Models;

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

//Response - UTC
//{"results":{"sunrise":"2:57:51 PM","sunset":"1:47:07 AM","solar_noon":"8:22:29 PM","day_length":"10:49:16","civil_twilight_begin":"2:32:27 PM","civil_twilight_end":"2:12:31 AM","nautical_twilight_begin":"2:01:47 PM","nautical_twilight_end":"2:43:11 AM","astronomical_twilight_begin":"1:31:26 PM","astronomical_twilight_end":"3:13:32 AM"},"status":"OK","tzid":"UTC"}

//-Local
//{ "results":{ "sunrise":"9:57:51 AM","sunset":"8:47:07 PM","solar_noon":"3:22:29 PM","day_length":"10:49:16","civil_twilight_begin":"9:32:27 AM","civil_twilight_end":"9:12:31 PM","nautical_twilight_begin":"9:01:47 AM","nautical_twilight_end":"9:43:11 PM","astronomical_twilight_begin":"8:31:26 AM","astronomical_twilight_end":"10:13:32 PM"},"status":"OK","tzid":"America/New_York"}