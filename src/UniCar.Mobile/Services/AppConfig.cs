namespace UniCar.Mobile.Services;

public static class AppConfig
{
    // A API roda no seu computador (porta 5055, ver PASSO_A_PASSO.md).
    // - Emulador Android: 10.0.2.2 é o "localhost" do computador.
    // - Simulador iOS / Mac Catalyst: localhost funciona direto.
    // - Celular físico: troque pelo IP do seu computador na rede, ex.: http://192.168.0.10:5055/
    public static string ApiBaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5055/"
            : "http://localhost:5055/";
}
