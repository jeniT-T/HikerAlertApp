using System;
using System.Collections.Generic;
using System.Text;

namespace HikerAlertApp;

public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime RecordedAt { get; set; }

    public Location(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        RecordedAt = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Latitude}, {Longitude} - {RecordedAt}";
    }
}
