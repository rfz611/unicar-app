namespace UniCar.Api.Pricing;

/// <summary>
/// Valores do preço. Ficam no appsettings.json para ajustar sem mexer no código.
/// Referência: gasolina comum em Curitiba R$ 6,83/L (ANP, 28/08/2026).
/// </summary>
public class PricingOptions
{
    public const string Section = "Pricing";
    public decimal BaseFare { get; set; } = 2.00m;
    public decimal PerKm { get; set; } = 0.90m;
    public decimal PerMinute { get; set; } = 0.12m;
    /// <summary>Taxa do UniCar sobre o valor da corrida (0,15 = 15%).</summary>
    public decimal PlatformFee { get; set; } = 0.15m;
    public decimal MinimumFare { get; set; } = 7.00m;
    /// <summary>Carona marcada sai mais barata (0,90 = 10% de desconto).</summary>
    public decimal ScheduledMultiplier { get; set; } = 0.90m;
    public decimal ScheduledMinimumFare { get; set; } = 6.00m;
}
