using System.ComponentModel.DataAnnotations;

namespace HikerAlertApp.Core
{
    public sealed class PasswordInput
    {
        public string Password { get; set; } = string.Empty;
    }

    public sealed class Authenticator
    {
        // At least 8 chars, one uppercase, one digit and one special character
        public Task<bool> AuthenticateAsync(string email, string password)
        {
            if (!IsValidEmail(email) || !IsValidPassword(password))
                return Task.FromResult(false);

            // Placeholder: accept any credentials that pass validation.
            // Replace with real user-store lookup and password verification as needed.
            return Task.FromResult(true);
        }

        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return new EmailAddressAttribute().IsValid(email);
        }

        private static bool IsValidPassword(string? password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            return PasswordPolicy.IsValid(password);
        }

        // Overload that accepts a credentials DTO and validates via data annotations
        public Task<bool> AuthenticateAsync(CredentialsInput credentials)
        {
            if (credentials is null)
                return Task.FromResult(false);

            if (!credentials.TryValidate(out var results))
                return Task.FromResult(false);

            return AuthenticateAsync(credentials.Email, credentials.Password);
        }
    }
}