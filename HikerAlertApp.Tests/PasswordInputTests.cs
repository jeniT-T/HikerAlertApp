using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HikerAlertApp.Core;

namespace HikerAlertApp.Tests
{
    [TestClass]
    public class PasswordInputTests
    {
        [TestMethod]
        public void PasswordInput_Defaults_Password_NotNull()
        {
            var dto = new PasswordInput();
            Assert.IsNotNull(dto.Password, "Password should default to empty string, not null");
            Assert.AreEqual(string.Empty, dto.Password);
        }

        [TestMethod]
        public async Task Authenticator_Rejects_Whitespace_Email_Or_Password_StringOverload()
        {
            var auth = new Authenticator();

            var r1 = await auth.AuthenticateAsync("   ", "A1bcd!ef");
            Assert.IsFalse(r1);

            var r2 = await auth.AuthenticateAsync("user@example.com", "   ");
            Assert.IsFalse(r2);

            var r3 = await auth.AuthenticateAsync("   ", "   ");
            Assert.IsFalse(r3);
        }

        [TestMethod]
        public async Task Authenticator_Rejects_Invalid_Email()
        {
            var auth = new Authenticator();

            var result1 = await auth.AuthenticateAsync(null!, "P@ssw0rd");
            Assert.IsFalse(result1);

            var result2 = await auth.AuthenticateAsync(string.Empty, "P@ssw0rd");
            Assert.IsFalse(result2);

            var result3 = await auth.AuthenticateAsync("not-an-email", "P@ssw0rd");
            Assert.IsFalse(result3);
        }

        [TestMethod]
        public async Task Authenticator_Rejects_Invalid_Passwords()
        {
            var auth = new Authenticator();
            const string email = "test@example.com";

            // null/empty
            var r1 = await auth.AuthenticateAsync(email, string.Empty);
            Assert.IsFalse(r1);

            // too short
            var r2 = await auth.AuthenticateAsync(email, "Aa1!");
            Assert.IsFalse(r2);

            // missing uppercase
            var r3 = await auth.AuthenticateAsync(email, "password1!");
            Assert.IsFalse(r3);

            // missing digit
            var r4 = await auth.AuthenticateAsync(email, "Password!");
            Assert.IsFalse(r4);

            // missing special
            var r5 = await auth.AuthenticateAsync(email, "Password1");
            Assert.IsFalse(r5);
        }

        [TestMethod]
        public async Task Authenticator_Accepts_Valid_Passwords()
        {
            var auth = new Authenticator();
            const string email = "user@example.com";

            // exactly 8 chars, has uppercase, digit, special
            var good1 = "A1bcd!ef"; // length 8
            var r1 = await auth.AuthenticateAsync(email, good1);
            Assert.IsTrue(r1);

            // longer valid password
            var good2 = "StrongP@ssw0rd2024!";
            var r2 = await auth.AuthenticateAsync(email, good2);
            Assert.IsTrue(r2);
        }

        [TestClass]
        public class CredentialsInputTests
        {
            [TestMethod]
            public void CredentialsInput_Defaults_Are_Empty()
            {
                var dto = new CredentialsInput();
                Assert.AreEqual(string.Empty, dto.Email);
                Assert.AreEqual(string.Empty, dto.Password);
            }
        }
    }
}
