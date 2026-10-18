using System.Text.Json;

namespace HikerAlertApp.Core;

//Persistent alert queue so undelivered SOS alerts survive an app restart.
public sealed class JsonFileSosAlertQueue : ISosAlertQueue
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonFileSosAlertQueue(string filePath)
        => _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

    public async Task<IReadOnlyList<PendingSosAlert>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await ReadAsync(cancellationToken)).ToArray();
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
            var items = await ReadAsync(cancellationToken);
            items.Add(alert);
            await WriteAsync(items, cancellationToken);
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
            var items = await ReadAsync(cancellationToken);
            var index = items.FindIndex(item => item.Id == alertId);

            if (index < 0)
            {
                return false;
            }

            items.RemoveAt(index);
            await WriteAsync(items, cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => (await GetAllAsync(cancellationToken)).Count;

    private async Task<List<PendingSosAlert>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        return JsonSerializer.Deserialize<List<PendingSosAlert>>(json, SerializerOptions) ?? [];
    }

    private async Task WriteAsync(List<PendingSosAlert> items, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(items, SerializerOptions);
        var temporaryPath = _filePath + ".tmp";

        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}
