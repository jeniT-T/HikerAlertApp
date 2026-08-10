using System;
using System.Collections.Generic;
using System.Text;

using HikerAlertApp;

AlertManager alertManager = new AlertManager();

List<NearbyDevice> nearbyDevices = new List<NearbyDevice>
{
    new NearbyDevice("Hiker Phone A", 15),
    new NearbyDevice("Hiker Phone B", 40),
    new NearbyDevice("Emergency Beacon", 75)
};

int nextAlertId = 1;

bool running = true;

while (running)
{
    Console.WriteLine("\n==========================");
    Console.WriteLine("      HIKER ALERT APP");
    Console.WriteLine("==========================");

    Console.WriteLine("1. Create SOS Alert");
    Console.WriteLine("2. Find Nearby Devices");
    Console.WriteLine("3. View SOS Alerts");
    Console.WriteLine("4. Exit");

    Console.Write("\nSelect an option: ");

    string? choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            CreateSOSAlert();
            break;

        case "2":
            ShowNearbyDevices();
            break;

        case "3":
            alertManager.ViewAlerts();
            break;

        case "4":
            running = false;
            Console.WriteLine("Closing Hiker Alert App...");
            break;

        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

void CreateSOSAlert()
{
    Console.Write("\nEnter emergency message: ");
    string message = Console.ReadLine() ?? "Emergency SOS";

    Console.Write("Enter latitude: ");
    double latitude = Convert.ToDouble(Console.ReadLine());

    Console.Write("Enter longitude: ");
    double longitude = Convert.ToDouble(Console.ReadLine());

    Location location = new Location(latitude, longitude);

    SOSAlert alert = new SOSAlert(
        nextAlertId,
        message,
        location
    );

    nextAlertId++;

    alertManager.AddAlert(alert);

    alert.DisplayAlert();

    Console.WriteLine("\nSearching for nearby devices...");

    if (nearbyDevices.Count > 0)
    {
        NearbyDevice closestDevice = nearbyDevices
            .OrderBy(device => device.Distance)
            .First();

        Console.WriteLine(
            $"Nearby device found: {closestDevice.DeviceName}"
        );

        alertManager.SendAlert(alert, closestDevice);
    }
    else
    {
        Console.WriteLine("No nearby devices found.");
    }
}

void ShowNearbyDevices()
{
    Console.WriteLine("\n===== NEARBY DEVICES =====");

    foreach (NearbyDevice device in nearbyDevices)
    {
        device.DisplayDevice();
    }
}