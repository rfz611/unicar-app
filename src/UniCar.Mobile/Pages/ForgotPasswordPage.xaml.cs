using UniCar.Mobile.Services;

namespace UniCar.Mobile.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly AuthService _auth;

    public ForgotPasswordPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        ErrorBanner.IsVisible = false;

        var email = EmailField.Text.Trim();
        if (!Validators.IsValidEmail(email))
        {
            EmailField.ErrorText = "Informe um e-mail válido.";
            return;
        }

        SendButton.IsEnabled = false;
        SendButton.Text = "Enviando...";
        var result = await _auth.ForgotPasswordAsync(email);
        SendButton.IsEnabled = true;
        SendButton.Text = "Enviar código";

        if (!result.Success)
        {
            ErrorLabel.Text = result.Error;
            ErrorBanner.IsVisible = true;
            return;
        }

        // devCode só existe com a API em Development (sem servidor de e-mail); em produção vem vazio.
        var route = $"reset?email={Uri.EscapeDataString(email)}";
        if (!string.IsNullOrEmpty(result.Data?.DevCode))
        {
            route += $"&devCode={Uri.EscapeDataString(result.Data.DevCode)}";
        }

        await Shell.Current.GoToAsync(route);
    }
}
