namespace HikerAlert;

public partial class SignUpPage : ContentPage
{
	public SignUpPage()
	{
		InitializeComponent();
	}

    
    private async void OnSignUpClicked(object? sender, EventArgs e)
    {
        
        await DisplayAlertAsync("Success", "Account Created!", "OK");
        await Shell.Current.GoToAsync("MainPage");
    }

    
    private async void OnLoginClicked(object? sender, EventArgs e)
    {
   
        await Shell.Current.GoToAsync("..");
    }
}