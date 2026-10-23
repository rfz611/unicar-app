using System.Net;
using System.Net.Http.Json;
using UniCar.Api.Common;
using UniCar.Api.Driver;
using UniCar.Api.Users;
using UniCar.Tests.Infra;
using Xunit;

namespace UniCar.Tests;

[Collection(ApiCollection.Name)]
public class DriverTests(ApiFactory api)
{
    [Fact]
    public async Task Do_pedido_ate_motorista_aprovado()
    {
        var (bruno, user) = await api.NewUserAsync(fullName: "Bruno Lima");
        var cnh = TestDriver.Cnh();

        // dados sem fotos: ainda não vai para análise
        var res = await bruno.PostAsJsonAsync("/api/driver/application", TestDriver.Request(cnh));
        var app = await res.Content.ReadFromJsonAsync<DriverApplicationDto>();
        Assert.Equal("None", app!.Status);
        Assert.Equal(new[] { "cnhPhoto", "selfie" }, app.Missing);
        Assert.Equal(Cnh.Masked(cnh), app.CnhMasked);

        await bruno.PostAsync("/api/driver/application/cnh-photo", TestDriver.File(TestDriver.Png));
        res = await bruno.PostAsync("/api/driver/application/selfie", TestDriver.File(TestDriver.Png));
        app = await res.Content.ReadFromJsonAsync<DriverApplicationDto>();
        Assert.Equal("InReview", app!.Status);
        Assert.Empty(app.Missing);
        Assert.Equal("InReview", (await bruno.GetFromJsonAsync<UserDto>("/api/users/me"))!.DriverStatus);

        // admin vê o pedido completo e a foto
        var (admin, adminUser) = await api.NewUserAsync(fullName: "Admin Unicar");
        await api.MakeAdminAsync(adminUser.Id);
        var pending = await admin.GetFromJsonAsync<List<AdminDriverDto>>("/api/admin/drivers");
        var item = Assert.Single(pending!, p => p.UserId == user.Id);
        Assert.Equal(cnh, item.Cnh);
        var photo = await admin.GetAsync($"/api/admin/drivers/{user.Id}/cnh-photo");
        Assert.Equal("image/png", photo.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TestDriver.Png, await photo.Content.ReadAsByteArrayAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/drivers/{user.Id}/approve", null)).StatusCode);

        Assert.Equal("Approved", (await bruno.GetFromJsonAsync<UserDto>("/api/users/me"))!.DriverStatus);
        var vehicle = await bruno.GetFromJsonAsync<VehicleDto>("/api/driver/vehicle");
        Assert.Equal(("Onix 1.0", "Prata"), (vehicle!.Model, vehicle.Color));
        var again = await bruno.PostAsJsonAsync("/api/driver/application", TestDriver.Request());
        Assert.Equal("already_driver", (await again.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Recusado_ve_o_motivo_e_pode_reenviar()
    {
        var (carla, user) = await ApplyCompleteAsync();
        var (admin, adminUser) = await api.NewUserAsync();
        await api.MakeAdminAsync(adminUser.Id);

        await admin.PostAsJsonAsync($"/api/admin/drivers/{user.Id}/reject", new { reason = "Foto da CNH ilegível." });

        var app = await carla.GetFromJsonAsync<DriverApplicationDto>("/api/driver/application");
        Assert.Equal(("Rejected", "Foto da CNH ilegível."), (app!.Status, app.RejectionReason));

        // nova foto: volta para análise
        var res = await carla.PostAsync("/api/driver/application/cnh-photo", TestDriver.File(TestDriver.Png));
        app = await res.Content.ReadFromJsonAsync<DriverApplicationDto>();
        Assert.Equal("InReview", app!.Status);
        Assert.Null(app.RejectionReason);
    }

    [Fact]
    public async Task Quem_nao_e_admin_nao_ve_o_painel()
    {
        var (client, other) = await api.NewUserAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/drivers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/admin/drivers/{other.Id}/approve", null)).StatusCode);
    }

    [Theory]
    [InlineData("62472927638", "12/2030", "ABC1D23", "invalid_cnh")]
    [InlineData(null, "01/2020", "ABC1D23", "cnh_expired")]
    [InlineData(null, "2030-12", "ABC1D23", "invalid_cnh_expiry")]
    [InlineData(null, "12/2030", "AB-12345", "invalid_plate")]
    public async Task Dados_invalidos_sao_recusados(string? cnh, string expiry, string plate, string code)
    {
        var (client, _) = await api.NewUserAsync();
        var res = await client.PostAsJsonAsync("/api/driver/application", TestDriver.Request(cnh, expiry, plate));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(code, (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Mesma_cnh_nao_vale_em_duas_contas()
    {
        var cnh = TestDriver.Cnh();
        var (a, _) = await api.NewUserAsync();
        var (b, _) = await api.NewUserAsync();
        await a.PostAsJsonAsync("/api/driver/application", TestDriver.Request(cnh));
        var res = await b.PostAsJsonAsync("/api/driver/application", TestDriver.Request(cnh));
        Assert.Equal("cnh_in_use", (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Foto_precisa_ser_imagem_de_verdade()
    {
        var (client, _) = await api.NewUserAsync();
        var noApp = await client.PostAsync("/api/driver/application/selfie", TestDriver.File(TestDriver.Png));
        Assert.Equal("no_application", (await noApp.Content.ReadFromJsonAsync<ApiError>())?.Code);

        await client.PostAsJsonAsync("/api/driver/application", TestDriver.Request());
        var fake = await client.PostAsync("/api/driver/application/selfie", TestDriver.File("não sou foto"u8.ToArray(), "x.png"));
        Assert.Equal("invalid_image", (await fake.Content.ReadFromJsonAsync<ApiError>())?.Code);

        var big = new byte[ImageUpload.MaxBytes + 1];
        TestDriver.Png.CopyTo(big, 0);
        var tooBig = await client.PostAsync("/api/driver/application/selfie", TestDriver.File(big));
        Assert.Equal("file_too_large", (await tooBig.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Em_analise_nao_troca_os_dados()
    {
        var (client, _) = await ApplyCompleteAsync();
        var res = await client.PostAsJsonAsync("/api/driver/application", TestDriver.Request());
        Assert.Equal("in_review", (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    private async Task<(HttpClient, UserDto)> ApplyCompleteAsync()
    {
        var (client, user) = await api.NewUserAsync();
        await client.PostAsJsonAsync("/api/driver/application", TestDriver.Request());
        await client.PostAsync("/api/driver/application/cnh-photo", TestDriver.File(TestDriver.Png));
        await client.PostAsync("/api/driver/application/selfie", TestDriver.File(TestDriver.Png));
        return (client, user);
    }
}
