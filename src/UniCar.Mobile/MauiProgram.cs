using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using UniCar.Mobile.Pages;
using UniCar.Mobile.Services;

namespace UniCar.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Remove a linha/borda nativa do Entry: o visual do campo é desenhado pelo FormField (estilo Uber).
        EntryHandler.Mapper.AppendToMapping("UniCarNoBorder", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });

        // Serviços
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<TravelApiClient>();
        builder.Services.AddSingleton<ActivityService>();
        builder.Services.AddSingleton<RouteQuoteService>();
        builder.Services.AddSingleton<NegotiationService>();
        builder.Services.AddTransient<ActivitiesPage>();
        builder.Services.AddSingleton(new RideService(new HttpClient
        {
            BaseAddress = new Uri(AppConfig.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        }));

        // Shell e páginas (resolvidos por injeção de dependência)
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RoleSelectionPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<ResetPasswordPage>();
        builder.Services.AddTransient<HomePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
