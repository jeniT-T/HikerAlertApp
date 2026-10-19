using System;

namespace HikerAlertApp.Core;

/// <summary>Abstraction over alert transmission connectivity (NFR01).</summary>
public interface IConnectivityProvider
{
    bool HasInternetConnection { get; }
}

/// <summary>A single transmission channel for SOS alerts.</summary>
public interface IAlertRelay
{
    string ChannelName { get; }

    Task<bool> RelayAsync(SosAlert alert, CancellationToken cancellationToken = default);
}

/// <summary>Resolves the ranger service endpoint (DEF11).</summary>
public interface ISosEndpointProvider
{
    Uri? GetEndpoint();
}

/// <summary>Speed-dials emergency services through the platform dialer (FR11).</summary>
public interface IEmergencyDialer
{
    Task<bool> DialAsync(string emergencyNumber, CancellationToken cancellationToken = default);
}
