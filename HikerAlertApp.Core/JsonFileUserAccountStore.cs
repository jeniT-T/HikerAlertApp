using System.Text.Json;

namespace HikerAlertApp.Core;

//Persists accounts to a JSON file so sign-up survives app restarts (FR01).
public sealed class JsonFileUserAccountStore : IUserAccountStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, UserAccount>? _cache;

    public JsonFileUserAccountStore(string filePath, IClock? clock = null)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _clock = clock ?? SystemClock.Instance;
    }

    public async Task<bool> ExistsAsync(string normalisedEmail, CancellationToken cancellationToken = default)
        => (await LoadAsync(cancellationToken)).ContainsKey(normalisedEmail);

    public async Task CreateAsync(string normalisedEmail, string passwordHash, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadCoreAsync(cancellationToken);
            accounts[normalisedEmail] = new UserAccount(
                normalisedEmail, passwordHash, UserRole.Hiker, _clock.UtcNow);
            await SaveCoreAsync(accounts, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount?> FindAsync(string normalisedEmail, CancellationToken cancellationToken = default)
        => (await LoadAsync(cancellationToken)).TryGetValue(normalisedEmail, out var account)
            ? account
            : null;

    public async Task<bool> DeleteAsync(string normalisedEmail, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadCoreAsync(cancellationToken);
            if (!accounts.Remove(normalisedEmail))
            {
                return false;
            }

            await SaveCoreAsync(accounts, cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, UserAccount>> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, UserAccount>> LoadCoreAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        if (!File.Exists(_filePath))
        {
            _cache = new Dictionary<string, UserAccount>(StringComparer.Ordinal);
            return _cache;
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        var loaded = JsonSerializer.Deserialize<List<UserAccount>>(json, SerializerOptions) ?? [];

        _cache = loaded.ToDictionary(account => account.normalisedEmail, StringComparer.Ordinal);
        return _cache;
    }

    private async Task SaveCoreAsync(Dictionary<string, UserAccount> accounts, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(accounts.Values.ToList(), SerializerOptions);
        var temporaryPath = _filePath + ".tmp";

        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}
