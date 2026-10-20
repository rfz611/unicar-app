using Microsoft.Extensions.DependencyInjection;

namespace UniCar.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        _services = services;
    }

    // Resolve o Shell depois de carregar os recursos globais do Application.
    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(_services.GetRequiredService<AppShell>());
}
