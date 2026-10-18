namespace HikerAlertApp.Core;

public sealed record SosDispatchResult(
    SosAlert Alert,
    bool DeliveredImmediately,
    bool QueuedForRetry,
    string Channel);


/// create the alert, queue it, attempt delivery,and leave it queued when offline 

public sealed class SosDispatchService
{
    private readonly SosAlertFactory _alertFactory;
    private readonly ISosAlertQueue _queue;
    private readonly IAlertRelay _relay;
    private readonly IConnectivityProvider _connectivity;
    private readonly IClock _clock;

    public SosDispatchService(
        SosAlertFactory alertFactory,
        ISosAlertQueue queue,
        IAlertRelay relay,
        IConnectivityProvider connectivity,
        IClock? clock = null)
    {
        _alertFactory = alertFactory ?? throw new ArgumentNullException(nameof(alertFactory));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _relay = relay ?? throw new ArgumentNullException(nameof(relay));
        _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        _clock = clock ?? SystemClock.Instance;
    }


    public async Task<SosDispatchResult> SubmitAsync(
        SosAlertInput input,
        CancellationToken cancellationToken = default)
    {
        var alert = await _alertFactory.CreateAsync(input, cancellationToken);

        var pending = new PendingSosAlert(Guid.NewGuid(), alert, 0, _clock.UtcNow);
        await _queue.EnqueueAsync(pending, cancellationToken);

        var delivered = false;

        if (_connectivity.HasInternetConnection)
        {
            delivered = await _relay.RelayAsync(alert, cancellationToken);

            if (delivered)
            {
                await _queue.RemoveAsync(pending.Id, cancellationToken);
            }
        }

        return new SosDispatchResult(
            alert,
            DeliveredImmediately: delivered,
            QueuedForRetry: !delivered,
            Channel: delivered ? _relay.ChannelName : "queued-on-device");
    }

    /// retries every queued alert. Called when connectivity is restored
    public async Task<int> FlushPendingAsync(CancellationToken cancellationToken = default)
    {
        if (!_connectivity.HasInternetConnection)
        {
            return 0;
        }

        var pending = await _queue.GetAllAsync(cancellationToken);
        var deliveredCount = 0;

        foreach (var item in pending)
        {
            if (await _relay.RelayAsync(item.Alert, cancellationToken))
            {
                await _queue.RemoveAsync(item.Id, cancellationToken);
                deliveredCount++;
            }
        }

        return deliveredCount;
    }
}
