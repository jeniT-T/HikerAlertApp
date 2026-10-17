using HikerAlertApp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HikerAlertApp.Tests;

[TestClass]
public class HikeCheckInServiceTests
{
    [TestMethod]
    public async Task StartHike_SchedulesFirstCheckInOneHourLater()
    {
        var (service, clock, scheduler) = CreateService();

        var dueAt = await service.StartHikeAsync();

        Assert.AreEqual(clock.UtcNow.AddHours(1), dueAt);
        Assert.AreEqual(dueAt, scheduler.ScheduledTimes[0]);
        Assert.IsNull(service.GetDuePrompt());
    }

    [TestMethod]
    public async Task RespondYes_SchedulesNextCheckInOneHourLater()
    {
        var (service, clock, scheduler) = CreateService();
        var firstDueAt = await service.StartHikeAsync();

        clock.UtcNow = firstDueAt;
        var prompt = service.GetDuePrompt();

        Assert.IsNotNull(prompt);
        Assert.AreEqual("Are you okay?", prompt.Message);

        var response = await service.RespondAsync(isOkay: true);

        Assert.IsTrue(response.IsOkay);
        Assert.IsFalse(response.NeedsFollowUp);
        Assert.AreEqual(clock.UtcNow.AddHours(1), response.NextCheckInAt);
        Assert.AreEqual(2, scheduler.ScheduledTimes.Count);
    }

    [TestMethod]
    public async Task RespondNo_ReturnsOptionalNotesAndRequestsFollowUp()
    {
        var (service, clock, scheduler) = CreateService();
        var dueAt = await service.StartHikeAsync();

        clock.UtcNow = dueAt;
        Assert.IsNotNull(service.GetDuePrompt());

        var response = await service.RespondAsync(
            isOkay: false,
            notes: "I am tired and need help.");

        Assert.IsFalse(response.IsOkay);
        Assert.IsTrue(response.NeedsFollowUp);
        Assert.AreEqual("I am tired and need help.", response.Notes);
        Assert.IsNull(response.NextCheckInAt);

        // Only the initial reminder was scheduled; the app must handle follow-up.
        Assert.AreEqual(1, scheduler.ScheduledTimes.Count);
    }

    private static (HikeCheckInService Service, FakeClock Clock, RecordingScheduler Scheduler)
        CreateService()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero));
        var scheduler = new RecordingScheduler();
        return (new HikeCheckInService(clock, scheduler), clock, scheduler);
    }

    private sealed class FakeClock(DateTimeOffset initialTime) : IHikeClock
    {
        public DateTimeOffset UtcNow { get; set; } = initialTime;
    }

    private sealed class RecordingScheduler : IWellbeingCheckInScheduler
    {
        public List<DateTimeOffset> ScheduledTimes { get; } = [];

        public Task ScheduleAsync(
            DateTimeOffset dueAt,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScheduledTimes.Add(dueAt);
            return Task.CompletedTask;
        }
    }
}
