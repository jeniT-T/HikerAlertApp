using HikerAlertApp.Core;

namespace HikerAlertApp.Tests;

[TestClass]
public class LoginAttemptTrackerTests
{
    private const string Email = "USER@EXAMPLE.COM";

    [TestMethod]
    public void RegisterFailure_LocksOut_OnTenthAttempt()
    {
        var tracker = new LoginAttemptTracker();

        for (var i = 1; i < LoginAttemptTracker.Threshold; i++)
        {
            var outcome = tracker.RegisterFailure(Email);
            Assert.IsFalse(outcome.IsLockedOut, $"Attempt {i} should not lock the account.");
        }

        var tenth = tracker.RegisterFailure(Email);

        Assert.IsTrue(tenth.IsLockedOut);
        Assert.AreEqual(LoginAttemptTracker.Threshold, tenth.FailedAttempts);
        Assert.IsTrue(tracker.IsLockedOut(Email));
    }

    [TestMethod]
    public void RegisterSuccess_ClearsFailures()
    {
        var tracker = new LoginAttemptTracker();
        tracker.RegisterFailure(Email);
        tracker.RegisterFailure(Email);

        tracker.RegisterSuccess(Email);

        Assert.AreEqual(0, tracker.FailedAttemptsFor(Email));
        Assert.IsFalse(tracker.IsLockedOut(Email));
    }

    [TestMethod]
    public void IsLockedOut_IsScopedPerAccount()
    {
        var tracker = new LoginAttemptTracker();

        for (var i = 0; i < LoginAttemptTracker.Threshold; i++)
        {
            tracker.RegisterFailure(Email);
        }

        Assert.IsTrue(tracker.IsLockedOut(Email));
        Assert.IsFalse(tracker.IsLockedOut("OTHER@EXAMPLE.COM"));
    }
}
