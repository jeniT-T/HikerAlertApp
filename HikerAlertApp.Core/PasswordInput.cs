using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HikerAlertApp.Core
{
    // Simple DTO for password input
    public sealed class PasswordInput
    {   
        public string Password { get; set; } = string.Empty;
    }

    // Authenticator without interfaces
    public sealed class Authenticator
    {
        // At least 8 chars, one uppercase, one digit and one special character
        private static readonly Regex PasswordRegex = new(@"^(?=.{8,}$)(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).*", RegexOptions.Compiled);

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

            return PasswordRegex.IsMatch(password);
        }
    }
}