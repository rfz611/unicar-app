using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UniCar.Api.Places;
using Xunit;

namespace UniCar.Tests;

public class LocalPlacesTests
{
    [Theory]
    [InlineData("politecnico", "UFPR - Centro Politécnico")]
    [InlineData("puc", "PUCPR - Pontifícia Universidade Católica do Paraná")]
    [InlineData("agua verde", "Água Verde")]
    [InlineData("shopping est", "Shopping Estação")]
    public void Busca_sem_acento_e_por_pedaco(string q, string first) =>
        Assert.Equal(first, LocalPlaces.Search(q)[0].Name);

    [Fact]
    public void Busca_sem_resultado() => Assert.Empty(LocalPlaces.Search("xyzw"));

    [Fact]
    public void Campi_tem_as_coordenadas_do_seed()
    {
        var poli = LocalPlaces.Find("local:ufpr-politecnico")!;
        Assert.Equal((-25.4507, -49.2316), (poli.Latitude, poli.Longitude));
    }

    private static FallbackMapsClient Client(bool estimate, string key = "") => new(
        new GoogleMapsClient(new HttpClient(), Options.Create(new GoogleMapsOptions { ApiKey = key }), NullLogger<GoogleMapsClient>.Instance),
        Options.Create(new GoogleMapsOptions { ApiKey = key, EstimateWhenNotConfigured = estimate }));

    [Fact]
    public async Task Sem_chave_em_desenvolvimento_usa_o_catalogo()
    {
        var client = Client(estimate: true);
        var sug = await client.AutocompleteAsync("pucpr", "s", null);
        var place = await client.GetPlaceAsync(sug[0].PlaceId, "s");
        Assert.Equal("Prado Velho", place!.Neighborhood);

        var gps = await client.ReverseGeocodeAsync(new LatLng(-25.4422, -49.2888));
        Assert.Equal(("Batel", -25.4422), (gps!.Neighborhood, gps.Latitude));
    }

    [Fact]
    public async Task Sem_chave_em_producao_continua_503()
    {
        var client = Client(estimate: false);
        await Assert.ThrowsAsync<MapsNotConfiguredException>(() => client.AutocompleteAsync("pucpr", "s", null));
    }
}
