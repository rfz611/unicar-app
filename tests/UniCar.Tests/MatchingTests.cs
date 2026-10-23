using Microsoft.Extensions.Options;
using UniCar.Api.Places;
using UniCar.Api.Trips;
using Xunit;

namespace UniCar.Tests;

public class MatchingTests
{
    private static readonly LatLng Batel = new(-25.4420, -49.2890);
    private static readonly LatLng AguaVerde = new(-25.4560, -49.2810);
    private static readonly LatLng Politecnico = new(-25.4507, -49.2316);
    private static readonly LatLng SantaFelicidade = new(-25.4030, -49.3300);

    [Fact]
    public void Mesmo_caminho_cabe_no_desvio()
    {
        var d = RideMatching.Compute(Batel, Politecnico, AguaVerde, Politecnico);
        Assert.True(d.Fits);
        Assert.InRange(d.ExtraKm, 0.1, 2);
    }

    [Fact]
    public void Sentido_contrario_nao_cabe()
    {
        // motorista do Politécnico para Santa Felicidade; passageira quer ir para o Politécnico
        var d = RideMatching.Compute(Politecnico, SantaFelicidade, AguaVerde, Politecnico);
        Assert.False(d.Fits);
    }

    [Fact]
    public async Task Sem_chave_do_Google_estima_so_se_configurado()
    {
        var noKey = new GoogleMapsClient(new HttpClient(), Options.Create(new GoogleMapsOptions()), Microsoft.Extensions.Logging.Abstractions.NullLogger<GoogleMapsClient>.Instance);

        var dev = new RouteService(noKey, Options.Create(new GoogleMapsOptions { EstimateWhenNotConfigured = true }));
        var (route, estimated) = await dev.GetAsync(Batel, Politecnico);
        Assert.True(estimated);
        Assert.InRange(route!.Km, 5, 12);

        var prod = new RouteService(noKey, Options.Create(new GoogleMapsOptions()));
        await Assert.ThrowsAsync<MapsNotConfiguredException>(() => prod.GetAsync(Batel, Politecnico));
    }
}
