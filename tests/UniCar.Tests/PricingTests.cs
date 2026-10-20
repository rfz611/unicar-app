using Microsoft.Extensions.Options;
using UniCar.Api.Pricing;
using Xunit;

namespace UniCar.Tests;

public class PricingTests
{
    private static PricingService Service() => new(Options.Create(new PricingOptions()));

    [Fact]
    public void Corrida_media_pedir_agora()
    {
        // 2 + 7,2 x 0,90 + 21 x 0,12 = 11,00 -> +15% = 12,65 -> arredonda para 12,50
        var q = Service().Quote(7.2, 21, RideMode.Now);
        Assert.Equal(12.50m, q.Total);
        Assert.Equal(q.Total, q.DriverAmount + q.PlatformAmount);
    }

    [Fact]
    public void Carona_marcada_sai_mais_barata()
    {
        var q = Service().Quote(7.2, 21, RideMode.Scheduled);
        Assert.Equal(11.50m, q.Total);
    }

    [Fact]
    public void Corrida_curta_cobra_o_minimo()
    {
        Assert.Equal(7.00m, Service().Quote(1, 4, RideMode.Now).Total);
        Assert.Equal(6.00m, Service().Quote(1, 4, RideMode.Scheduled).Total);
    }

    [Fact]
    public void Distancia_negativa_e_erro()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Service().Quote(-1, 5, RideMode.Now));
    }
}
