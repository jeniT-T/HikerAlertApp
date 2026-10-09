using HikerAlertApp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HikerAlertApp.Tests;

[TestClass]
public class HikerProfileServiceTests
{
    [TestMethod]
    public async Task SaveAsync_TrimsValuesAndAllowsOmittedFields()
    {
        var repository = new InMemoryProfileRepository();
        var service = new HikerProfileService(repository, new FakeAccountDeletionService());

        await service.SaveAsync(new HikerProfile(
            Name: "  Alex Hiker  ",
            MobilePhone: " 555-0100 ",
            EmergencyNote: "  Allergic to bees.  "));

        Assert.IsNotNull(repository.Profile);
        Assert.AreEqual("Alex Hiker", repository.Profile!.Name);
        Assert.AreEqual("555-0100", repository.Profile.MobilePhone);
        Assert.AreEqual("Allergic to bees.", repository.Profile.EmergencyNote);
        Assert.IsNull(repository.Profile.BloodType);
        Assert.IsNull(repository.Profile.EmergencyContactName);
        Assert.IsNull(repository.Profile.EmergencyContactPhone);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_RequestsFullAccountDeletion()
    {
        var deletion = new FakeAccountDeletionService();
        var service = new HikerProfileService(new InMemoryProfileRepository(), deletion);

        await service.DeleteAccountAsync();

        Assert.AreEqual(1, deletion.DeleteCalls);
    }

    private sealed class InMemoryProfileRepository : IHikerProfileRepository
    {
        public HikerProfile? Profile { get; private set; }

        public Task<HikerProfile?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Profile);

        public Task SaveAsync(
            HikerProfile profile,
            CancellationToken cancellationToken = default)
        {
            Profile = profile;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAccountDeletionService : IAccountDeletionService
    {
        public int DeleteCalls { get; private set; }

        public Task DeleteCurrentAccountAsync(CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            return Task.CompletedTask;
        }
    }
}
