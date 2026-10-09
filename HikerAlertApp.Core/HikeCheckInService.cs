using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HikerAlertApp.Core
{

    public interface IHikeClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public interface IWellbeingCheckInScheduler
    {
        Task ScheduleAsync(
            DateTimeOffset dueAt,
            CancellationToken cancellationToken = default);
    }

    public sealed record CheckInPrompt(string Message, DateTimeOffset DueAt);

    public sealed record CheckInResponse(
        bool IsOkay,
        string? Notes,
        DateTimeOffset? NextCheckInAt,
        bool NeedsFollowUp);

    public sealed class HikeCheckInService(
        IHikeClock clock,
        IWellbeingCheckInScheduler scheduler)
    {
        private bool _hikeStarted;
        private bool _waitingForResponse;
        private DateTimeOffset? _nextCheckInAt;

        public async Task<DateTimeOffset> StartHikeAsync(
            CancellationToken cancellationToken = default)
        {
            if (_hikeStarted)
                throw new InvalidOperationException("A hike is already in progress.");

            var firstCheckIn = clock.UtcNow.AddHours(1);
            await scheduler.ScheduleAsync(firstCheckIn, cancellationToken);

            _hikeStarted = true;
            _nextCheckInAt = firstCheckIn;
            return firstCheckIn;
        }

        // Call when the scheduled notification fires.
        public CheckInPrompt? GetDuePrompt()
        {
            if (!_hikeStarted || _waitingForResponse || _nextCheckInAt is null)
                return null;

            var dueAt = _nextCheckInAt.Value;
            if (clock.UtcNow < dueAt)
                return null;

            _waitingForResponse = true;
            return new CheckInPrompt("Are you okay?", dueAt);
        }

        public async Task<CheckInResponse> RespondAsync(
            bool isOkay,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            if (!_waitingForResponse)
                throw new InvalidOperationException("There is no check-in awaiting a response.");

            _waitingForResponse = false;

            if (!isOkay)
            {
                _nextCheckInAt = null;
                var optionalNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
                return new CheckInResponse(false, optionalNotes, null, NeedsFollowUp: true);
            }

            var nextCheckIn = clock.UtcNow.AddHours(1);
            await scheduler.ScheduleAsync(nextCheckIn, cancellationToken);
            _nextCheckInAt = nextCheckIn;

            return new CheckInResponse(true, null, nextCheckIn, NeedsFollowUp: false);
        }
    }
}
