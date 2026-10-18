namespace HikerAlertApp.Core;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
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

        var normalisedEmail = input.Email.Trim().ToUpperInvariant();

        if (await _accountStore.ExistsAsync(normalisedEmail, cancellationToken))
        {
            return new SignupResult(SignupStatus.ExistingEmail);
        }

        var passwordHash = _passwordHasher.Hash(input.Password);
        await _accountStore.CreateAsync(
            normalisedEmail,
            passwordHash,
            cancellationToken);

        return new SignupResult(SignupStatus.Created);
    }
}
