using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HikerAlertApp.Core
{
    public sealed class CredentialsInput
    {
        private const string PasswordPattern = "^(?=.{8,}$)(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9]).*";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [RegularExpression(PasswordPattern)]
        public string Password { get; set; } = string.Empty;

        public bool TryValidate(out IList<ValidationResult> results)
        {
            results = new List<ValidationResult>();

            if (string.IsNullOrWhiteSpace(Email))
            {
                results.Add(new ValidationResult("Email is required.", new[] { nameof(Email) }));
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                results.Add(new ValidationResult("Password is required.", new[] { nameof(Password) }));
            }

            if (results.Count > 0)
            {
                return false;
            }

            var context = new ValidationContext(this);
            return Validator.TryValidateObject(
                this,
                context,
                results,
                validateAllProperties: true);
        }
    }
}
