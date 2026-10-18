namespace HikerAlertApp.Core;

public sealed record PendingSosAlert(
    Guid Id,
    SosAlert Alert,
    int DeliveryAttempts,
    DateTimeOffset QueuedAtUtc);

public interface ISosAlertQueue
{
    Task<IReadOnlyList<PendingSosAlert>> GetAllAsync(CancellationToken cancellationToken = default);
    Task EnqueueAsync(PendingSosAlert alert, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

//NFR01 — alerts are held locally when there is no connectivity.
public sealed class InMemorySosAlertQueue : ISosAlertQueue
{
    private readonly List<PendingSosAlert> _items = [];
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<PendingSosAlert>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _items.ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EnqueueAsync(PendingSosAlert alert, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _items.Add(alert);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> RemoveAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var index = _items.FindIndex(item => item.Id == alertId);
            if (index < 0)
            {
                return false;
            }

            _items.RemoveAt(index);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _items.Count;
        }
        finally
        {
            _gate.Release();
        }
    }
}
