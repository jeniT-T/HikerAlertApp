using System.ComponentModel.DataAnnotations;

namespace HikerAlertApp.Core;

public sealed class SignupInput : IValidatableObject
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please retype your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PasswordPolicy.IsValid(Password))
        {
            yield return new ValidationResult(
                "Password must be at least 8 characters and include an uppercase letter, a digit, and a special character.",
                new[] { nameof(Password) });
        }
    }

    public bool TryValidate(out IList<ValidationResult> results)
    {
        results = new List<ValidationResult>();

        return Validator.TryValidateObject(
            this,
            new ValidationContext(this),
            results,
            validateAllProperties: true);
    }
}
