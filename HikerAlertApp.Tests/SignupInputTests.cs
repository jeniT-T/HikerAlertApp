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

    // SignUpAsync service tests
    [TestMethod]
    public async Task SignUpAsync_ReturnsShowLoginInput_WhenEmailAlreadyExists()
    {
        var store = new InMemoryAccountStore("USER@EXAMPLE.COM");
        var service = CreateService(store);

        var result = await service.SignUpAsync(CreateValidInput("user@example.com"));

        Assert.AreEqual(SignupStatus.ExistingEmail, result.Status);
        Assert.IsTrue(result.ShowLoginInput);
        Assert.IsFalse(result.Succeeded);
        Assert.IsEmpty(store.CreatedAccounts);
    }

    [TestMethod]
    public async Task SignUpAsync_CreatesAccount_WhenInputIsValidAndEmailIsNew()
    {
        var store = new InMemoryAccountStore();
        var service = CreateService(store);

        var result = await service.SignUpAsync(CreateValidInput(" User@example.com "));

        Assert.AreEqual(SignupStatus.Created, result.Status);
        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.ShowLoginInput);
        Assert.HasCount(1, store.CreatedAccounts);
        Assert.AreEqual("USER@EXAMPLE.COM", store.CreatedAccounts[0].Email);
        Assert.AreEqual("hashed:A1bcd!ef", store.CreatedAccounts[0].PasswordHash);
    }

    [TestMethod]
    public async Task SignUpAsync_DoesNotCreateAccount_WhenInputIsInvalid()
    {
        var store = new InMemoryAccountStore();
        var service = CreateService(store);
        var input = CreateValidInput("not-an-email");

        var result = await service.SignUpAsync(input);

        Assert.AreEqual(SignupStatus.InvalidInput, result.Status);
        Assert.IsEmpty(store.CreatedAccounts);
    }

    [TestMethod]
    public async Task SignUpAsync_RejectsNullInput()
    {
        var service = CreateService(new InMemoryAccountStore());

        var result = await service.SignUpAsync(null);

        Assert.AreEqual(SignupStatus.InvalidInput, result.Status);
    }

    private static SignupService CreateService(InMemoryAccountStore store) =>
        new(store, new TestPasswordHasher());

    private static SignupInput CreateValidInput(string email) => new()
    {
        Email = email,
        Password = "A1bcd!ef",
        ConfirmPassword = "A1bcd!ef"
    };

    private sealed class InMemoryAccountStore : IUserAccountStore
    {
        private readonly HashSet<string> _existingEmails;

        public InMemoryAccountStore(params string[] existingEmails)
        {
            _existingEmails = existingEmails
                .Select(email => email.Trim().ToUpperInvariant())
                .ToHashSet(StringComparer.Ordinal);
        }

        public List<CreatedAccount> CreatedAccounts { get; } = new();

        public Task<bool> ExistsAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_existingEmails.Contains(normalizedEmail));
        }

        public Task CreateAsync(
            string normalizedEmail,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            _existingEmails.Add(normalizedEmail);
            CreatedAccounts.Add(new CreatedAccount(normalizedEmail, passwordHash));
            return Task.CompletedTask;
        }
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
    }

    private sealed record CreatedAccount(string Email, string PasswordHash);
}
