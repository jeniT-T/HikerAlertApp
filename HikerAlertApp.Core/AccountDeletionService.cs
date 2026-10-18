namespace HikerAlertApp.Core;

public enum DeletionRequestStatus
{
    Requested,
    NotFound
}

public sealed record DeletionRequestResult(
    DeletionRequestStatus Status,
    bool RequiresConfirmation);

//TC05 — request on deleting the user account.
public sealed class AccountDeletionService
{
    private readonly IUserAccountStore _accountStore;
    private readonly HikerProfileService _profileService;

    public AccountDeletionService(IUserAccountStore accountStore, HikerProfileService profileService)
    {
        _accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    }

    public async Task<DeletionRequestResult> RequestDeletionAsync(
        string normalisedEmail,
        CancellationToken cancellationToken = default)
    {
        var exists = await _accountStore.ExistsAsync(normalisedEmail, cancellationToken);

        return exists
            ? new DeletionRequestResult(DeletionRequestStatus.Requested, RequiresConfirmation: true)
            : new DeletionRequestResult(DeletionRequestStatus.NotFound, RequiresConfirmation: false);
    }

    public async Task<bool> ConfirmDeletionAsync(
        string normalisedEmail,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _accountStore.DeleteAsync(normalisedEmail, cancellationToken);
        _profileService.Delete(normalisedEmail);
        return deleted;
    }
}
