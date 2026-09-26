namespace HikerAlertApp.Core;

public interface IUserAccountStore
{
    Task<bool> ExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        string normalizedEmail,
        string passwordHash,
        CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
}

public enum SignupStatus
{
    Created,
    InvalidInput,
    ExistingEmail
}

public sealed record SignupResult(SignupStatus Status)
{
    public bool Succeeded => Status == SignupStatus.Created;

    public bool ShowLoginInput => Status == SignupStatus.ExistingEmail;
}

public sealed class SignupService
{
    private readonly IUserAccountStore _accountStore;
    private readonly IPasswordHasher _passwordHasher;

    public SignupService(
        IUserAccountStore accountStore,
        IPasswordHasher passwordHasher)
    {
        _accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<SignupResult> SignUpAsync(
        SignupInput? input,
        CancellationToken cancellationToken = default)
    {
        if (input is null || !input.TryValidate(out _))
        {
            return new SignupResult(SignupStatus.InvalidInput);
        }

        var normalizedEmail = input.Email.Trim().ToUpperInvariant();

        if (await _accountStore.ExistsAsync(normalizedEmail, cancellationToken))
        {
            return new SignupResult(SignupStatus.ExistingEmail);
        }

        var passwordHash = _passwordHasher.Hash(input.Password);
        await _accountStore.CreateAsync(
            normalizedEmail,
            passwordHash,
            cancellationToken);

        return new SignupResult(SignupStatus.Created);
    }
}
