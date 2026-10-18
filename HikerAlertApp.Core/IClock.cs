using System;
using System.Collections.Generic;
using System.Text;

namespace HikerAlertApp.Core
//This interface is used to abstract the system clock for testing purposes. It allows for injecting a mock clock during unit tests to control the current time.
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new();
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
