namespace HikerAlertApp.Core;

public sealed record LoginAttemptOutcome(
    bool IsLockedOut,
    int FailedAttempts,
    int RemainingBeforeWarning);

/// Implements the FR01 / TC04 rule: warning displayed after 10 failed attempts.
public sealed class LoginAttemptTracker
{
    public const int Threshold = 10;

    private readonly Dictionary<string, int> _failedAttempts = new(StringComparer.Ordinal);

    public int FailedAttemptsFor(string normalisedEmail)
        => _failedAttempts.TryGetValue(normalisedEmail, out var count) ? count : 0;

    public bool IsLockedOut(string normalisedEmail)
        => FailedAttemptsFor(normalisedEmail) >= Threshold;

    public LoginAttemptOutcome RegisterFailure(string normalisedEmail)
    {
        var count = FailedAttemptsFor(normalisedEmail) + 1;
        _failedAttempts[normalisedEmail] = count;

        return new LoginAttemptOutcome(
            IsLockedOut: count >= Threshold,
            FailedAttempts: count,
            RemainingBeforeWarning: Math.Max(0, Threshold - count));
    }

    public void RegisterSuccess(string normalisedEmail) => _failedAttempts.Remove(normalisedEmail);

    public void Reset(string normalisedEmail) => _failedAttempts.Remove(normalisedEmail);
}
