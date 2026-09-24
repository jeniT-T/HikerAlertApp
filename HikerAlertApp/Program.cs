using System;
using System.Collections.Generic;
using System.Text;

using HikerAlertApp;

AlertManager alertManager = new AlertManager();     

Location deviceCurrentLocation = new Location(34.0522, -1   18.2437);

int nextAlertId = 1;

bool running = true;

while (running)
{
    Console.WriteLine("\n==========================");
    Console.WriteLine("      HIKER ALERT APP");
    Console.WriteLine("==========================");

    Console.WriteLine("1. Create SOS Alert");
    Console.WriteLine("2. View SOS Alerts");
    Console.WriteLine("3. Exit");

    Console.Write("\nSelect an option: ");

    string? choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            CreateSOSAlert();
            break;

        case "2":
            alertManager.ViewAlerts();
            break;

        case "3":
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

    Location location = deviceCurrentLocation;
    Console.WriteLine($"Using current device location: {location}");

    SOSAlert alert = new SOSAlert(
        nextAlertId,
        message,
        location
    );

    nextAlertId++;

    alertManager.AddAlert(alert);

    alert.DisplayAlert();

    
}
