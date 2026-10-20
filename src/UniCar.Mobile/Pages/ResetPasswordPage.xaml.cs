using UniCar.Mobile.Services;

namespace UniCar.Mobile.Pages;

[QueryProperty(nameof(Email), "email")]
[QueryProperty(nameof(DevCode), "devCode")]
public partial class ResetPasswordPage : ContentPage
{
    private readonly AuthService _auth;
    private string _email = string.Empty;

    public ResetPasswordPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    /// <summary>E-mail para o qual o código foi enviado (vem da tela anterior).</summary>
    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            SubtitleLabel.Text = $"Digite o código enviado para {value} e escolha uma nova senha.";
        }
    }

    /// <summary>Preenchido só quando a API está em Development (sem envio real de e-mail).</summary>
    public string DevCode
    {
        set
        {
            if (string.IsNullOrEmpty(value)) return;
            DevLabel.Text = $"Modo desenvolvimento: o e-mail não é enviado. Seu código é {value}.";
            DevBanner.IsVisible = true;
        }
    }

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        ErrorBanner.IsVisible = false;

        var valid = true;
        if (Validators.OnlyDigits(CodeField.Text).Length != 6)
        {
            CodeField.ErrorText = "Digite o código de 6 dígitos.";
            valid = false;
        }
        if (PasswordField.Text.Length < 6)
        {
            PasswordField.ErrorText = "A senha precisa ter no mínimo 6 caracteres.";
            valid = false;
        }
        if (ConfirmField.Text != PasswordField.Text)
        {
            ConfirmField.ErrorText = "As senhas não são iguais.";
            valid = false;
        }
        if (!valid) return;

        ResetButton.IsEnabled = false;
        ResetButton.Text = "Salvando...";
        var result = await _auth.ResetPasswordAsync(_email, CodeField.Text, PasswordField.Text);
        ResetButton.IsEnabled = true;
        ResetButton.Text = "Redefinir senha";

        if (!result.Success)
        {
            ErrorLabel.Text = result.Error;
            ErrorBanner.IsVisible = true;
            return;
        }

        // Volta ao login com um aviso de sucesso.
        await Shell.Current.GoToAsync("//welcome/login?notice=reset");
    }

    private async void OnResendTapped(object? sender, TappedEventArgs e)
    {
        ErrorBanner.IsVisible = false;

        var result = await _auth.ForgotPasswordAsync(_email);
        if (!result.Success)
        {
            ErrorLabel.Text = result.Error;
            ErrorBanner.IsVisible = true;
            return;
        }

        CodeField.Text = string.Empty;
        if (!string.IsNullOrEmpty(result.Data?.DevCode))
        {
            DevLabel.Text = $"Modo desenvolvimento: o e-mail não é enviado. Seu novo código é {result.Data.DevCode}.";
            DevBanner.IsVisible = true;
        }
        else
        {
            DevLabel.Text = "Enviamos um novo código para o seu e-mail.";
            DevBanner.IsVisible = true;
        }
    }
}
