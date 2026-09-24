using System.Text.RegularExpressions;

namespace HikerAlertApp.Core;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;

    private static readonly Regex PasswordRegex = new(
        @"^(?=.{8,}$)(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValid(string? password)
    {
        return !string.IsNullOrWhiteSpace(password)
            && PasswordRegex.IsMatch(password);
    }
}
