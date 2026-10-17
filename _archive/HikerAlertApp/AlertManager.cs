using System;
using System.Collections.Generic;
using System.Text;
namespace HikerAlertApp;

public class AlertManager
{
    private List<SOSAlert> alerts = new List<SOSAlert>();

    public void AddAlert(SOSAlert alert)
    {
        alerts.Add(alert);

        Console.WriteLine("\nSOS Alert successfully created.");
    }

    public void ViewAlerts()
    {
        if (alerts.Count == 0)
        {
            Console.WriteLine("\nNo SOS alerts have been created.");
            return;
        }

        Console.WriteLine("\n===== SAVED ALERTS =====");

        foreach (SOSAlert alert in alerts)
        {
            alert.DisplayAlert();
        }
    }

    public void SendAlert(SOSAlert alert)
    {
        Console.WriteLine($"\nSending alert ...");
        Console.WriteLine("SOS Alert successfully forwarded.");
    }
}