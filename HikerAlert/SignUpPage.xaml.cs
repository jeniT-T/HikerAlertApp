using HikerAlertApp.Core;

namespace HikerAlert;

public partial class SignUpPage : ContentPage
{
    private readonly SignupService _signupService;

    public SignUpPage(SignupService signupService)
    {
        InitializeComponent();
        _signupService = signupService;
    }

    private async void OnSignUpClicked(object? sender, EventArgs e)
    {
        var input = new SignupInput
        {
            Email = EmailEntry.Text ?? string.Empty,
            Password = PasswordEntry.Text ?? string.Empty,
            ConfirmPassword = ConfirmPasswordEntry.Text ?? string.Empty
        };

        var result = await _signupService.SignUpAsync(input);

        switch (result.Status)
        {
            case SignupStatus.Created:
                await DisplayAlertAsync("Account created", "You can now sign in.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                break;

            case SignupStatus.ExistingEmail:
                await DisplayAlertAsync(
                    "Email already registered",
                    "An account already exists for that email address. Try signing in instead.",
                    "OK");
                break;

            default:
                await DisplayAlertAsync(
                    "Check your details",
                    "Passwords must be at least 8 characters and include an uppercase letter, a digit, and a special character.",
                    "OK");
                break;
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
