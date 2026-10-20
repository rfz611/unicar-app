namespace UniCar.Mobile.Controls;

public partial class BackButton : ContentView
{
    public BackButton()
    {
        InitializeComponent();
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("..");
}
