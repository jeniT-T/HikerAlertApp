using HikerAlertApp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HikerAlertApp.Tests;

[TestClass]
public class SignupInputTests
{
    [TestMethod]
    public void TryValidate_AcceptsValidEmailPasswordAndConfirmation()
    {
        var input = new SignupInput
        {
            Email = "user@example.com",
            Password = "A1bcd!ef",
            ConfirmPassword = "A1bcd!ef"
        };

        var valid = input.TryValidate(out var results);

        Assert.IsTrue(valid);
        Assert.IsEmpty(results);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not-an-email")]
    [DataRow("user@")]
    [DataRow("@example.com")]
    public void TryValidate_RejectsInvalidEmail(string email)
    {
        var input = CreateValidInput();
        input.Email = email;

        var valid = input.TryValidate(out var results);

        Assert.IsFalse(valid);
        Assert.IsTrue(results.Any(result =>
            result.MemberNames.Contains(nameof(SignupInput.Email))));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Aa1!")]
    [DataRow("password1!")]
    [DataRow("Password!")]
    [DataRow("Password1")]
    public void TryValidate_RejectsPasswordsThatDoNotMeetRequirements(string password)
    {
        var input = CreateValidInput();
        input.Password = password;
        input.ConfirmPassword = password;

        var valid = input.TryValidate(out var results);

        Assert.IsFalse(valid);
        Assert.IsTrue(results.Any(result =>
            result.MemberNames.Contains(nameof(SignupInput.Password))));
    }

    [TestMethod]
    public void TryValidate_RejectsDifferentConfirmationPassword()
    {
        var input = CreateValidInput();
        input.ConfirmPassword = "Different1!";

        var valid = input.TryValidate(out var results);

        Assert.IsFalse(valid);
        Assert.IsTrue(results.Any(result =>
            result.MemberNames.Contains(nameof(SignupInput.ConfirmPassword))));
    }

    private static SignupInput CreateValidInput() => new()
    {
        Email = "user@example.com",
        Password = "A1bcd!ef",
        ConfirmPassword = "A1bcd!ef"
    };
}
