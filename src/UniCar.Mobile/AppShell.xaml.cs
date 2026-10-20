using UniCar.Mobile.Pages;
using UniCar.Mobile.Services;

namespace UniCar.Mobile;

public partial class AppShell : Shell
{
    public AppShell(SessionService session)
    {
        InitializeComponent();

        // Rotas empilhadas (push) a partir da tela de boas-vindas
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("role", typeof(RoleSelectionPage));
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("forgot", typeof(ForgotPasswordPage));
        Routing.RegisterRoute("reset", typeof(ResetPasswordPage));

        // Já logado? Abre direto na Home.
        if (session.IsLoggedIn)
        {
            CurrentItem = HomeTabs;
        }
    }
}
