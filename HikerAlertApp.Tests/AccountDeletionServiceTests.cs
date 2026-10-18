using HikerAlertApp.Core;

namespace HikerAlertApp.Tests;

[TestClass]
public class AccountDeletionServiceTests
{
    private const string Email = "USER@EXAMPLE.COM";

    [TestMethod]
    public async Task RequestDeletionAsync_ReturnsRequested_WhenAccountExists()
    {
        var store = new InMemoryUserAccountStore();
        await store.CreateAsync(Email, "hash");
        var service = new AccountDeletionService(store, new HikerProfileService());

        var result = await service.RequestDeletionAsync(Email);

        Assert.AreEqual(DeletionRequestStatus.Requested, result.Status);
        Assert.IsTrue(result.RequiresConfirmation);
    }

    [TestMethod]
    public async Task RequestDeletionAsync_ReturnsNotFound_WhenAccountMissing()
    {
        var service = new AccountDeletionService(
            new InMemoryUserAccountStore(),
            new HikerProfileService());

        var result = await service.RequestDeletionAsync(Email);

        Assert.AreEqual(DeletionRequestStatus.NotFound, result.Status);
        Assert.IsFalse(result.RequiresConfirmation);
    }

    [TestMethod]
    public async Task ConfirmDeletionAsync_RemovesAccountAndProfile()
    {
        var store = new InMemoryUserAccountStore();
        await store.CreateAsync(Email, "hash");

        var profiles = new HikerProfileService();
        profiles.Save(new HikerProfile(Email, Name: "Sam"));

        var service = new AccountDeletionService(store, profiles);

        var deleted = await service.ConfirmDeletionAsync(Email);

        Assert.IsTrue(deleted);
        Assert.IsFalse(await store.ExistsAsync(Email));
        Assert.IsFalse(profiles.TryGet(Email, out _));
    }
}
