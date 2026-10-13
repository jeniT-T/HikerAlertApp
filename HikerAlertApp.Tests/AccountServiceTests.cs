using HikerAlertApp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HikerAlertApp.Tests
{
    [TestClass]
    public class AccountServiceTests
    {
        [TestMethod]
        public async Task SignUp_WithValidEmailAndPassword_CreatesAccountAndAllowsLogin()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);

            var signUpResult = await service.SignUpAsync("Hiker@example.com", "TrailRun9!");
            var loginResult = await service.LoginAsync("Hiker@example.com", "TrailRun9!");

            Assert.IsTrue(signUpResult.Succeeded);
            Assert.IsNotNull(repository.Account);
            Assert.AreEqual("hiker@example.com", repository.Account!.Email);
            Assert.IsTrue(loginResult.Succeeded);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("not-an-email@")]
        public async Task SignUp_WithInvalidEmail_IsRejected(string email)
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);

            var result = await service.SignUpAsync(email, "TrailRun9!");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("Enter a valid email address.", result.Message);
            Assert.IsNull(repository.Account);
        }

        [DataTestMethod]
        [DataRow("Ab1!xyz")]       // Fewer than 8 characters
        [DataRow("trailrun9!")]    // No uppercase letter
        [DataRow("TrailRun!!")]    // No number
        [DataRow("TrailRun99")]    // No special character
        public async Task SignUp_WithPasswordMissingRequirements_IsRejected(string password)
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);

            var result = await service.SignUpAsync("hiker@example.com", password);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Message.Contains("at least 8 characters", StringComparison.Ordinal));
            Assert.IsNull(repository.Account);
        }

        [TestMethod]
        public async Task Login_WithEmailOrPasswordThatDoesNotMatchSignup_FailsAndAsksUserToTryAgain()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);
            await service.SignUpAsync("hiker@example.com", "TrailRun9!");

            var wrongEmailResult = await service.LoginAsync("other@example.com", "TrailRun9!");
            var wrongPasswordResult = await service.LoginAsync("hiker@example.com", "TrailRun8!");

            Assert.IsFalse(wrongEmailResult.Succeeded);
            Assert.IsFalse(wrongPasswordResult.Succeeded);
            Assert.AreEqual(wrongEmailResult.Message, wrongPasswordResult.Message);
            StringAssert.Contains(wrongPasswordResult.Message, "try again");
        }

        [TestMethod]
        public async Task SignUp_WhenAccountAlreadyExists_IsRejected()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);
            await service.SignUpAsync("hiker@example.com", "TrailRun9!");

            var result = await service.SignUpAsync("another@example.com", "TrailRun8!");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("An account is already registered.", result.Message);
        }

        [TestMethod]
        public async Task Login_WhenNoAccountHasBeenCreated_IsRejected()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);

            var result = await service.LoginAsync("person@example.com", "TrailRun9!");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(
                "Login failed. Check your email and password, then try again.",
                result.Message);
            Assert.IsNull(repository.Account);
        }

        [TestMethod]
        public async Task Login_AfterMoreThanTenFailedAttempts_ReturnsWarning()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);
            await service.SignUpAsync("hiker@example.com", "TrailRun9!");

            for (var attempt = 0; attempt < 10; attempt++)
            {
                var result = await service.LoginAsync("hiker@example.com", "WrongPassword9!");

                Assert.IsFalse(result.Succeeded);
                Assert.AreEqual(
                    "Login failed. Check your email and password, then try again.",
                    result.Message);
            }

            var warningResult = await service.LoginAsync("hiker@example.com", "WrongPassword9!");

            Assert.IsFalse(warningResult.Succeeded);
            StringAssert.Contains(warningResult.Message, "Login failed. Too many failed login attempts. Please try again later.");
        }

        private sealed class InMemoryAccountRepository : IAccountRepository
        {
            public AccountCredential? Account { get; private set; }

            public Task<AccountCredential?> GetAsync() =>
                Task.FromResult(Account);

            public Task SaveAsync(AccountCredential account)
            {
                Account = account;
                return Task.CompletedTask;
            }
        }
    }
}
