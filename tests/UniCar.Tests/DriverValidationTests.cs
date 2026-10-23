using UniCar.Api.Common;
using UniCar.Api.Driver;
using Xunit;

namespace UniCar.Tests;

public class DriverValidationTests
{
    private static readonly DateOnly Today = new(2026, 10, 23);

    [Theory]
    [InlineData("62472927637", true)]
    [InlineData("624.729.276-37", true)]
    [InlineData("62472927638", false)]
    [InlineData("11111111111", false)]
    [InlineData("6247292763", false)]
    [InlineData(null, false)]
    public void Cnh_confere_digitos(string? cnh, bool valido) => Assert.Equal(valido, Cnh.IsValid(cnh));

    [Fact]
    public void Cnh_mascarada_mostra_so_o_final() => Assert.Equal("*******7637", Cnh.Masked("62472927637"));

    [Theory]
    [InlineData("ABC1234", true)]
    [InlineData("abc-1234", true)]
    [InlineData("ABC1D23", true)]
    [InlineData("abc 1d23", true)]
    [InlineData("AB12345", false)]
    [InlineData("ABC12D3", false)]
    [InlineData("ABCD123", false)]
    public void Placa_antiga_ou_mercosul(string placa, bool valida) => Assert.Equal(valida, Plate.IsValid(placa));

    [Fact]
    public void Validade_vale_ate_o_fim_do_mes()
    {
        Assert.True(Cnh.TryParseExpiry("02/2028", out var fim));
        Assert.Equal(new DateOnly(2028, 2, 29), fim);
        Assert.False(Cnh.TryParseExpiry("13/2028", out _));
        Assert.False(Cnh.TryParseExpiry("2028-02", out _));
    }

    [Fact]
    public void Cnh_que_vence_este_mes_ainda_vale_e_a_do_mes_passado_nao()
    {
        Assert.Null(Validate("10/2026"));
        Assert.Equal("cnh_expired", Validate("09/2026")?.Code);
    }

    [Fact]
    public void Modelo_e_cor_sao_obrigatorios()
    {
        var erro = DriverController.Validate(new DriverApplicationRequest("62472927637", "12/2030", "ABC1D23", "", "Prata"),
            Today, out _, out _, out _);
        Assert.Equal("invalid_model", erro?.Code);
    }

    private static ApiError? Validate(string expiry) =>
        DriverController.Validate(new DriverApplicationRequest("62472927637", expiry, "ABC1D23", "Onix", "Prata"),
            Today, out _, out _, out _);
}
