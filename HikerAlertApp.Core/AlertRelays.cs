using System.Net.Http.Json;

namespace HikerAlertApp.Core;

//Posts alerts to the ranger service over HTTPS.
public sealed class HttpAlertRelay : IAlertRelay
{
    private readonly HttpClient _httpClient;
    private readonly ISosEndpointProvider _endpointProvider;

    public HttpAlertRelay(HttpClient httpClient, ISosEndpointProvider endpointProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
    }

    public string ChannelName => "https";

    public async Task<bool> RelayAsync(SosAlert alert, CancellationToken cancellationToken = default)
    {
        var endpoint = _endpointProvider.GetEndpoint();

        if (endpoint is null)
        {
            return false;
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(endpoint, alert, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}

/// <summary>
/// Simulated device-to-device relay for FR09. The platform Bluetooth radio is not
/// exercised; peers are registered in memory so the relay path can be demonstrated
/// and tested without hardware.
/// </summary>
public sealed class SimulatedBluetoothRelay : IAlertRelay
{
    private readonly List<Action<SosAlert>> _peers = [];

    public string ChannelName => "bluetooth-simulated";

    public int RegisteredPeers => _peers.Count;

    public void RegisterPeer(Action<SosAlert> onAlertReceived)
    {
        ArgumentNullException.ThrowIfNull(onAlertReceived);
        _peers.Add(onAlertReceived);
    }

    public Task<bool> RelayAsync(SosAlert alert, CancellationToken cancellationToken = default)
    {
        if (_peers.Count == 0)
        {
            return Task.FromResult(false);
        }

        foreach (var peer in _peers)
        {
            peer(alert);
        }

        return Task.FromResult(true);
    }
}

/// Tries each channel in order and reports success if any delivers.
public sealed class CompositeAlertRelay : IAlertRelay
{
    private readonly IReadOnlyList<IAlertRelay> _relays;

    public CompositeAlertRelay(params IAlertRelay[] relays)
        => _relays = relays ?? throw new ArgumentNullException(nameof(relays));

    public string ChannelName => string.Join("+", _relays.Select(relay => relay.ChannelName));

    public async Task<bool> RelayAsync(SosAlert alert, CancellationToken cancellationToken = default)
    {
        foreach (var relay in _relays)
        {
            if (await relay.RelayAsync(alert, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }
}
