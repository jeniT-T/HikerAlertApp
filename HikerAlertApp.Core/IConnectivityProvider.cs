using System;

namespace HikerAlertApp.Core;

/// Abstraction over alert transmission connectivity (NFR01).
public interface IConnectivityProvider
{
    bool HasInternetConnection { get; }
}

/// A single transmission channel for SOS alerts.
public interface IAlertRelay
{
    string ChannelName { get; }

    Task<bool> RelayAsync(SosAlert alert, CancellationToken cancellationToken = default);
}

///Resolves the ranger service endpoint 
public interface ISosEndpointProvider
{
    Uri? GetEndpoint();
}

/// Speed-dials emergency services through the platform dialer
public interface IEmergencyDialer
{
    Task<bool> DialAsync(string emergencyNumber, CancellationToken cancellationToken = default);
}
