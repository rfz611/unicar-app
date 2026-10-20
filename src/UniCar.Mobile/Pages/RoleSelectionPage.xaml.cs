using UniCar.Mobile.Controls;

namespace UniCar.Mobile.Pages;

public partial class RoleSelectionPage : ContentPage
{
    private string? _selectedRole;

    public RoleSelectionPage()
    {
        InitializeComponent();
    }

    private void OnRoleTapped(object? sender, EventArgs e)
    {
        if (sender is not RoleCard tapped) return;

        _selectedRole = tapped.Value;
        PassengerCard.IsSelected = ReferenceEquals(tapped, PassengerCard);
        DriverCard.IsSelected = ReferenceEquals(tapped, DriverCard);
        BothCard.IsSelected = ReferenceEquals(tapped, BothCard);
        ContinueButton.IsEnabled = true;
    }

    private async void OnContinueClicked(object? sender, EventArgs e)
    {
        if (_selectedRole is null) return;
        await Shell.Current.GoToAsync($"register?role={_selectedRole}");
    }
}
