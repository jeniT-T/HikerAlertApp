using HikerAlertApp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HikerAlertApp.Tests
{
    [TestClass]
    public class CredentialsInputTests
    {
        [TestMethod]
        public void CredentialsInput_TryValidate_Rejects_InvalidEmail()
        {
            foreach (var email in new string?[] { null, "", "   ", "\t\n" })
            {
                var dto = new CredentialsInput
                {
                    Email = email!,
                    Password = "A1bcd!ef"
                };

                var valid = dto.TryValidate(out var results);

                Assert.IsFalse(valid, $"Expected invalid when Email='{email ?? "null"}'");
                Assert.IsNotEmpty(results);
            }
        }

        [TestMethod]
        public void CredentialsInput_TryValidate_Rejects_InvalidPassword()
        {
            foreach (var password in new string?[] { null, "", "   ", "\t\n" })
            {
                var dto = new CredentialsInput
                {
                    Email = "user@example.com",
                    Password = password!
                };

                var valid = dto.TryValidate(out var results);

                Assert.IsFalse(valid, $"Expected invalid when Password='{password ?? "null"}'");
                Assert.IsNotEmpty(results);
            }
        }

        [TestMethod]
        public void CredentialsInput_TryValidate_Accepts_Valid_Credentials()
        {
            var dto = new CredentialsInput
            {
                Email = "user@example.com",
                Password = "A1bcd!ef"
            };

            var valid = dto.TryValidate(out var results);

            Assert.IsTrue(valid);
            Assert.IsNotNull(results);
            Assert.IsEmpty(results);
        }
    }
}
