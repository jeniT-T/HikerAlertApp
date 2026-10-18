namespace HikerAlertApp.Core;

public interface IUserAccountStore
{
    Task<bool> ExistsAsync(
          string normalisedEmail,
          CancellationToken cancellationToken = default);

    Task CreateAsync(
        string normalisedEmail,
        string passwordHash,
        CancellationToken cancellationToken = default);

    Task<UserAccount?> FindAsync(
        string normalisedEmail,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string normalisedEmail,
        CancellationToken cancellationToken = default);
}
