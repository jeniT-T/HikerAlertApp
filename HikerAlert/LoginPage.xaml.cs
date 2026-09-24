namespace HikerAlert;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    // Matches Clicked="OnSignUpClicked" in XAML
    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(SignUpPage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    // Matches Clicked="OnLoginClick" in XAML
    private async void OnLoginClick(object sender, EventArgs e)
    {
        // Use // to go to the root of the app (Home)
        await Shell.Current.GoToAsync("MainPage");
    }
}
