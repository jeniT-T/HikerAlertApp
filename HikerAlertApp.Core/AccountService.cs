using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HikerAlertApp.Core
{
    public interface IAccountRepository
    {
        Task<AccountCredential?> GetAsync();
        Task SaveAsync(AccountCredential account);
    }

    public sealed record AccountCredential(string Email, string Salt, string PasswordHash);

    public sealed record AccountOperationResult(bool Succeeded, string Message);

    public sealed class AccountService(IAccountRepository repository)
    {
        private const int Pbkdf2Iterations = 210_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        private const string LoginFailureMessage =
            "Login failed. Check your email and password, then try again.";

        private static readonly Regex PasswordRequirements = new(
            @"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).{8,}$",
            RegexOptions.Compiled);

        public async Task<AccountOperationResult> SignUpAsync(string? email, string? password)
        {
            var normalizedEmail = NormalizeEmail(email);
            var suppliedPassword = password ?? string.Empty;

            if (!new EmailAddressAttribute().IsValid(normalizedEmail))
            {
                return new(false, "Enter a valid email address.");
            }

            if (!PasswordRequirements.IsMatch(suppliedPassword))
            {
                return new(
                    false,
                    "Password must be at least 8 characters and include an uppercase letter, a number, and a special character.");
            }

            if (await repository.GetAsync() is not null)
            {
                return new(false, "An account is already registered.");
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = HashPassword(suppliedPassword, salt);

            await repository.SaveAsync(new AccountCredential(
                normalizedEmail,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash)));

            return new(true, "Account created. You can now log in.");
        }

        public async Task<AccountOperationResult> LoginAsync(string? email, string? password)
        {
            var account = await repository.GetAsync();
            var normalizedEmail = NormalizeEmail(email);

            if (account is null ||
                !string.Equals(normalizedEmail, account.Email, StringComparison.Ordinal) ||
                !VerifyPassword(password ?? string.Empty, account))
            {
                return new(false, LoginFailureMessage);
            }

            return new(true, "Login successful.");
        }

        private static string NormalizeEmail(string? email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        private static byte[] HashPassword(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

        private static bool VerifyPassword(string password, AccountCredential account)
        {
            try
            {
                var salt = Convert.FromBase64String(account.Salt);
                var expectedHash = Convert.FromBase64String(account.PasswordHash);
                var actualHash = HashPassword(password, salt);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
