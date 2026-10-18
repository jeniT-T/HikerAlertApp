using System;
using System.Collections.Generic;
using System.Text;

namespace HikerAlertApp.Core
{
    //Abstraction over alert transmission connectivity. 
    public interface IConnectivityProvider
    {
        bool HasInternetConnection { get; }
    }

    public interface AlertReply
    {
        string ChannelName { get; }
        Task<bool> RelayAsync(SosAlert alert, CancellationToken cancellationToken = default);       
    }

    //resolves the ranger service endpoint
    public interface SosEndpointProvider
    {
        Uri?GetEndPoints();
    }

    //Ranger's speed dial to emeregency services.
    public interface EmergencyServiceProvider
    {
        Task<bool> DialAsync(string dialEmergencyNumber, CancellationToken cancellationToken = default);
    }

}
