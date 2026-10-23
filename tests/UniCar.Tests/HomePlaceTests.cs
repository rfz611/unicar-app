using System.Net;
using System.Net.Http.Json;
using UniCar.Api.Common;
using UniCar.Api.Users;
using UniCar.Tests.Infra;
using Xunit;

namespace UniCar.Tests;

[Collection(ApiCollection.Name)]
public class HomePlaceTests(ApiFactory api)
{
    private static readonly object Batel = new
    {
        address = "R. Bispo Dom José, 2000 - Batel, Curitiba - PR", neighborhood = "Batel",
        googlePlaceId = "abc", latitude = -25.4420, longitude = -49.2890
    };

    [Fact]
    public async Task Casa_comeca_sem_definir()
    {
        var (client, _) = await api.NewUserAsync();
        var res = await client.GetAsync("/api/me/places/home");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("home_not_set", (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Definir_trocar_e_apagar_casa()
    {
        var (client, _) = await api.NewUserAsync();

        var put = await client.PutAsJsonAsync("/api/me/places/home", Batel);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var home = await client.GetFromJsonAsync<SavedPlaceDto>("/api/me/places/home");
        Assert.Equal(("Home", "Casa", "Batel"), (home!.Kind, home.Label, home.Neighborhood));

        // PUT de novo atualiza a mesma casa em vez de criar outra
        await client.PutAsJsonAsync("/api/me/places/home", new
        {
            address = "Av. Sete de Setembro, 3000 - Rebouças", neighborhood = "Rebouças", latitude = -25.44, longitude = -49.26
        });
        var updated = await client.GetFromJsonAsync<SavedPlaceDto>("/api/me/places/home");
        Assert.Equal(home.Id, updated!.Id);
        Assert.Equal("Rebouças", updated.Neighborhood);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/me/places/home")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/me/places/home")).StatusCode);
    }

    [Fact]
    public async Task Casa_fora_da_regiao_e_recusada()
    {
        var (client, _) = await api.NewUserAsync();
        var res = await client.PutAsJsonAsync("/api/me/places/home", new
        {
            address = "Av. Paulista, 1000 - São Paulo", latitude = -23.56, longitude = -46.65
        });
        Assert.Equal("outside_region", (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Casa_de_um_nao_aparece_para_outro()
    {
        var (ana, _) = await api.NewUserAsync();
        var (bia, _) = await api.NewUserAsync();
        await ana.PutAsJsonAsync("/api/me/places/home", Batel);
        Assert.Equal(HttpStatusCode.NotFound, (await bia.GetAsync("/api/me/places/home")).StatusCode);
    }
}
