namespace HikerAlertApp.Core;

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    UnknownAccount,
    LockedOut
}

public sealed record LoginResult(LoginStatus Status, UserRole? Role = null)
{
    public bool Succeeded => Status == LoginStatus.Success;

    public string Message => Status switch
    {
        LoginStatus.Success => "Signed in successfully.",
        LoginStatus.UnknownAccount => "No account found for that email address.",
        LoginStatus.LockedOut => $"Too many failed attempts. Your account is locked after {LoginAttemptTracker.Threshold} attempts.",
        _ => "The email address or password is incorrect."
    };
}


/// Real credential verification against the account store.
/// Replaces the placeholder Authenticator that accepted any well-formed credentials.

public sealed class AuthenticationService
{
    private readonly IUserAccountStore _accountStore;
    private readonly LoginAttemptTracker _attemptTracker;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationService(IUserAccountStore accountStore, LoginAttemptTracker attemptTracker, IPasswordHasher passwordHasher)
    {
        _accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        _attemptTracker = attemptTracker ?? throw new ArgumentNullException(nameof(attemptTracker));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<LoginResult> LoginAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var normalisedEmail = Normalize(email);

        if (_attemptTracker.IsLockedOut(normalisedEmail))
        {
            return new LoginResult(LoginStatus.LockedOut);
        }

        var account = await _accountStore.FindAsync(normalisedEmail, cancellationToken);

        if (account is null)
        {
            var unknownOutcome = _attemptTracker.RegisterFailure(normalisedEmail);
            return new LoginResult(unknownOutcome.IsLockedOut
                ? LoginStatus.LockedOut
                : LoginStatus.UnknownAccount);
        }

        if (!_passwordHasher.Verify(password, account.passwordHash))
        {
            var failureOutcome = _attemptTracker.RegisterFailure(normalisedEmail);
            return new LoginResult(failureOutcome.IsLockedOut
                ? LoginStatus.LockedOut
                : LoginStatus.InvalidCredentials);
        }

        _attemptTracker.RegisterSuccess(normalisedEmail);
        return new LoginResult(LoginStatus.Success, account.Role);
    }

    public static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
