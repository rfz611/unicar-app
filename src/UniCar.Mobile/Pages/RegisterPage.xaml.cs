using UniCar.Mobile.Models;
using UniCar.Mobile.Services;

namespace UniCar.Mobile.Pages;

[QueryProperty(nameof(Role), "role")]
public partial class RegisterPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly SessionService _session;
    private string _role = UserTypes.Both;

    public RegisterPage(AuthService auth, SessionService session)
    {
        InitializeComponent();
        _auth = auth;
        _session = session;
        ApplyRole(_role);
    }

    /// <summary>Recebido da tela anterior via Shell: register?role=DRIVER</summary>
    public string Role
    {
        get => _role;
        set
        {
            _role = value;
            ApplyRole(value);
        }
    }

    private void ApplyRole(string role)
    {
        RoleChip.Text = UserTypes.Describe(role);
        CnhSection.IsVisible = role is UserTypes.Driver or UserTypes.Both;
        RoleSubtitle.Text = role switch
        {
            UserTypes.Passenger => "Encontre colegas que passam pelo seu caminho.",
            UserTypes.Driver => "Ofereça vagas no seu carro e divida o trajeto com colegas.",
            _ => "Peça ou ofereça caronas, do jeito que for melhor para o seu dia."
        };
    }

    private void OnPhoneTextChanged(object? sender, TextChangedEventArgs e)
    {
        var formatted = Validators.FormatPhone(e.NewTextValue);
        if (formatted != PhoneField.Text) PhoneField.Text = formatted;
    }

    private void OnCnhExpiryTextChanged(object? sender, TextChangedEventArgs e)
    {
        var formatted = Validators.FormatDate(e.NewTextValue);
        if (formatted != CnhExpiryField.Text) CnhExpiryField.Text = formatted;
    }

    private bool NeedsCnh => _role is UserTypes.Driver or UserTypes.Both;

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        ErrorBanner.IsVisible = false;
        if (!ValidateForm()) return;

        var request = new RegisterRequest(
            Name: NameField.Text.Trim(),
            Email: EmailField.Text.Trim(),
            Password: PasswordField.Text,
            Phone: PhoneField.Text.Trim(),
            RaEnrollment: RaField.Text.Trim(),
            UserType: _role,
            CnhNumber: NeedsCnh ? Validators.OnlyDigits(CnhField.Text) : null,
            CnhCategory: NeedsCnh ? CnhCategoryField.Text.Trim().ToUpperInvariant() : null,
            CnhExpiry: NeedsCnh && Validators.TryParseDate(CnhExpiryField.Text, out var expiry)
                ? expiry.ToString("yyyy-MM-dd")
                : null);

        SetBusy(true);
        var result = await _auth.RegisterAsync(request);
        SetBusy(false);

        if (!result.Success || result.Data is null)
        {
            ErrorLabel.Text = result.Error;
            ErrorBanner.IsVisible = true;
            return;
        }

        await _session.SaveAsync(result.Data, _role);
        await Shell.Current.GoToAsync("//home/initial");
    }

    private bool ValidateForm()
    {
        var valid = true;

        var name = NameField.Text.Trim();
        if (name.Length < 3 || !name.Contains(' '))
        {
            NameField.ErrorText = "Informe seu nome completo.";
            valid = false;
        }

        if (!Validators.IsInstitutionalEmail(EmailField.Text))
        {
            EmailField.ErrorText = "Use seu e-mail institucional (@positivo.edu.br ou @up.edu.br).";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(RaField.Text))
        {
            RaField.ErrorText = "Informe seu RA / matrícula.";
            valid = false;
        }

        if (!Validators.IsValidPhone(PhoneField.Text))
        {
            PhoneField.ErrorText = "Informe um celular com DDD.";
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

        if (NeedsCnh && !ValidateCnh()) valid = false;

        return valid;
    }

    /// <summary>Confere formato, dígitos verificadores, categoria e validade da CNH (mesmas regras da API).</summary>
    private bool ValidateCnh()
    {
        var valid = true;

        if (!Validators.IsValidCnh(CnhField.Text))
        {
            CnhField.ErrorText = "Número da CNH inválido. Confira os 11 dígitos.";
            valid = false;
        }

        var category = CnhCategoryField.Text.Trim().ToUpperInvariant();
        if (category == "A")
        {
            CnhCategoryField.ErrorText = "A categoria A é só para moto. É preciso ter B ou superior.";
            valid = false;
        }
        else if (!Validators.IsCarCnhCategory(category))
        {
            CnhCategoryField.ErrorText = "Use B, C, D, E, AB, AC, AD ou AE.";
            valid = false;
        }

        if (!Validators.TryParseDate(CnhExpiryField.Text, out var expiry))
        {
            CnhExpiryField.ErrorText = "Informe a validade no formato dd/mm/aaaa.";
            valid = false;
        }
        else if (expiry < DateOnly.FromDateTime(DateTime.Today))
        {
            CnhExpiryField.ErrorText = "CNH vencida. Renove sua habilitação para oferecer caronas.";
            valid = false;
        }
        else if (expiry > DateOnly.FromDateTime(DateTime.Today).AddYears(11))
        {
            CnhExpiryField.ErrorText = "Validade da CNH inválida.";
            valid = false;
        }

        return valid;
    }

    private async void OnLoginTapped(object? sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//welcome/login");

    private void SetBusy(bool busy)
    {
        RegisterButton.IsEnabled = !busy;
        RegisterButton.Text = busy ? "Criando conta..." : "Criar conta";
    }
}
