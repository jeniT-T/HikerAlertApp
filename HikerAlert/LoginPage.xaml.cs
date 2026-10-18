using HikerAlertApp.Core;

namespace HikerAlert;

public partial class LoginPage : ContentPage
{
    private readonly AuthenticationService _authenticationService;

    public LoginPage(AuthenticationService authenticationService)
    {
        InitializeComponent();
        _authenticationService = authenticationService;
    }

    private async void OnLoginClick(object? sender, EventArgs e)
    {
        var result = await _authenticationService.LoginAsync(
            EmailEntry.Text,
            PasswordEntry.Text);

        if (!result.Succeeded)
        {
            await DisplayAlertAsync("Sign in failed", result.Message, "OK");
            return;
        }

        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnSignUpClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(SignUpPage));
}
