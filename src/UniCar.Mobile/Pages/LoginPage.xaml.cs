using UniCar.Mobile.Services;

namespace UniCar.Mobile.Pages;

[QueryProperty(nameof(Notice), "notice")]
public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly SessionService _session;

    public LoginPage(AuthService auth, SessionService session)
    {
        InitializeComponent();
        _auth = auth;
        _session = session;
    }

    /// <summary>Recebido via Shell (login?notice=reset) depois de redefinir a senha.</summary>
    public string Notice
    {
        set
        {
            if (value != "reset") return;
            NoticeLabel.Text = "Senha alterada! Entre com a nova senha.";
            NoticeBanner.IsVisible = true;
        }
    }

    private async void OnForgotPasswordTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("forgot");

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        ErrorBanner.IsVisible = false;

        var email = EmailField.Text.Trim();
        var password = PasswordField.Text;

        var valid = true;
        if (!Validators.IsValidEmail(email))
        {
            EmailField.ErrorText = "Informe um e-mail válido.";
            valid = false;
        }
        if (string.IsNullOrEmpty(password))
        {
            PasswordField.ErrorText = "Informe sua senha.";
            valid = false;
        }
        if (!valid) return;

        SetBusy(true);
        var result = await _auth.LoginAsync(email, password);
        SetBusy(false);

        if (!result.Success || result.Data is null)
        {
            ErrorLabel.Text = result.Error;
            ErrorBanner.IsVisible = true;
            return;
        }

        // O tipo de usuário (passageiro/motorista/ambos) vem no claim "role" do token.
        await _session.SaveAsync(result.Data, SessionService.ReadRoleFromToken(result.Data.Token));
        await Shell.Current.GoToAsync("//home/initial");
    }

    private async void OnCreateAccountTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("role");

    private void SetBusy(bool busy)
    {
        LoginButton.IsEnabled = !busy;
        LoginButton.Text = busy ? "Entrando..." : "Entrar";
    }
}
