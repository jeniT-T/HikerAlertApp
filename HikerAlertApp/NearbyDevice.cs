using System;
using System.Collections.Generic;
using System.Text;

namespace HikerAlertApp;

public class NearbyDevice
{
    public string DeviceName { get; set; }
    public double Distance { get; set; }

    public NearbyDevice(string deviceName, double distance)
    {
        DeviceName = deviceName;
        Distance = distance;
    }

    public void DisplayDevice()
    {
        Console.WriteLine($"{DeviceName} - {Distance} metres away");
    }
}