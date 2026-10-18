using System;
using System.Collections.Generic;
using System.Text;

namespace HikerAlertApp.Core
{
    public enum UserRole
    {
        Hiker,
        Ranger 
    }

    public sealed record UserAccount(
        string normalisedEmail,
        string passwordHash,
        UserRole Role,
        DateTimeOffset CreatedAtUtc);
}
