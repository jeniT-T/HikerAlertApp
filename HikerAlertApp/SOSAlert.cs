using System;
using System.Collections.Generic;
using System.Text;

namespace HikerAlertApp;

public class SOSAlert
{
    public int Id { get; set; }
    public string Message { get; set; }
    public Location LastKnownLocation { get; set; }
    public DateTime CreatedAt { get; set; }

    public SOSAlert(int id, string message, Location location)
    {
        Id = id;
        Message = message;
        LastKnownLocation = location;
        CreatedAt = DateTime.Now;
    }

    public void DisplayAlert()
    {
        Console.WriteLine("\n===== SOS ALERT =====");
        Console.WriteLine($"Alert ID: {Id}");
        Console.WriteLine($"Message: {Message}");
        Console.WriteLine($"Location: {LastKnownLocation.Latitude}, {LastKnownLocation.Longitude}");
        Console.WriteLine($"Created: {CreatedAt}");
        Console.WriteLine("=====================");
    }
}