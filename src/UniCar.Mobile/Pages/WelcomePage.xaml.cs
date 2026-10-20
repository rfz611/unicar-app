namespace UniCar.Mobile.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("login");

    private async void OnRegisterClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("role");
}
