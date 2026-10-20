using Microsoft.Extensions.Options;

namespace UniCar.Api.Pricing;

public enum RideMode { Now, Scheduled }

/// <summary>Quanto o passageiro paga, quanto vai para o motorista e quanto fica com o UniCar.</summary>
public record PriceQuote(decimal Total, decimal DriverAmount, decimal PlatformAmount, double Km, int Minutes, RideMode Mode);

public class PricingService(IOptions<PricingOptions> opt)
{
    private readonly PricingOptions _o = opt.Value;

    public PriceQuote Quote(double km, double minutes, RideMode mode)
    {
        if (km < 0 || minutes < 0) throw new ArgumentOutOfRangeException(nameof(km), "Distância e tempo não podem ser negativos.");

        var trip = _o.BaseFare + (decimal)km * _o.PerKm + (decimal)minutes * _o.PerMinute;
        var gross = trip * (1 + _o.PlatformFee);

        // O desconto da carona marcada vale sobre o valor da corrida, antes do mínimo;
        // senão o mínimo do "pedir agora" (R$ 7) impede de chegar ao mínimo da marcada (R$ 6).
        var total = mode == RideMode.Scheduled
            ? Math.Max(_o.ScheduledMinimumFare, RoundToHalf(gross * _o.ScheduledMultiplier))
            : Math.Max(_o.MinimumFare, RoundToHalf(gross));

        var driver = Math.Round(total / (1 + _o.PlatformFee), 2, MidpointRounding.AwayFromZero);
        return new PriceQuote(total, driver, total - driver, Math.Round(km, 1), (int)Math.Round(minutes), mode);
    }

    /// <summary>Arredonda para R$ 0,50 (12,30 vira 12,50; 12,20 vira 12,00).</summary>
    private static decimal RoundToHalf(decimal value) => Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2;
}
