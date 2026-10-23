using System.Net;
using System.Net.Http.Json;
using UniCar.Api.Common;
using UniCar.Api.Users;
using UniCar.Tests.Infra;
using Xunit;

namespace UniCar.Tests;

[Collection(ApiCollection.Name)]
public class ProfileTests(ApiFactory api)
{
    [Fact]
    public async Task Cadastro_ja_traz_o_campus_principal()
    {
        var (_, user) = await api.NewUserAsync();
        Assert.Equal("UFPR", user.University);
        Assert.Equal("Centro Politécnico", user.Campus?.Name);
        Assert.True(user.CanChooseCampus); // UFPR tem dois campi no seed
        Assert.True(user.ShowCourse);
    }

    [Fact]
    public async Task Escolher_outro_campus_da_propria_faculdade()
    {
        var (client, _) = await api.NewUserAsync();
        var unis = await client.GetFromJsonAsync<List<UniDto>>("/api/universities");
        var historico = unis!.Single(u => u.ShortName == "UFPR").Campuses.Single(c => c.Name == "Prédio Histórico");

        var res = await client.PutAsJsonAsync("/api/users/me/campus", new { campusId = historico.Id });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me");
        Assert.Equal("Prédio Histórico", me!.Campus?.Name);
        Assert.Equal(historico.Latitude, me.Campus!.Latitude);
    }

    [Fact]
    public async Task Nao_escolhe_campus_de_outra_faculdade()
    {
        var (client, _) = await api.NewUserAsync();
        var unis = await client.GetFromJsonAsync<List<UniDto>>("/api/universities");
        var puc = unis!.Single(u => u.ShortName == "PUCPR").Campuses[0];

        var res = await client.PutAsJsonAsync("/api/users/me/campus", new { campusId = puc.Id });
        Assert.Equal("invalid_campus", (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Preferencias_tem_padrao_e_podem_mudar()
    {
        var (client, _) = await api.NewUserAsync();
        var prefs = await client.GetFromJsonAsync<PreferencesDto>("/api/users/me/preferences");
        Assert.Equal(new PreferencesDto("System", true, false, true), prefs);

        var res = await client.PutAsJsonAsync("/api/users/me/preferences", new PreferencesDto("dark", false, true, false));
        Assert.Equal(new PreferencesDto("Dark", false, true, false), await res.Content.ReadFromJsonAsync<PreferencesDto>());

        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me");
        Assert.False(me!.ShowCourse);
    }

    [Fact]
    public async Task Tema_invalido_e_recusado()
    {
        var (client, _) = await api.NewUserAsync();
        var res = await client.PutAsJsonAsync("/api/users/me/preferences", new PreferencesDto("Roxo", true, false, true));
        Assert.Equal("invalid_theme", (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Sem_token_ou_com_token_de_cadastro_nao_abre_o_perfil()
    {
        var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);

        var email = $"t{Guid.NewGuid():N}"[..12] + "@ufpr.br";
        await client.PostAsJsonAsync("/api/auth/request-code", new { email });
        var verify = await client.PostAsJsonAsync("/api/auth/verify-code", new { email, code = api.Emails.LastCode(email) });
        var signup = (await verify.Content.ReadFromJsonAsync<UniCar.Api.Auth.VerifyCodeResponse>())!.SignupToken;
        client.DefaultRequestHeaders.Authorization = new("Bearer", signup);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    private record UniDto(int Id, string ShortName, List<CampusDto> Campuses);
}
