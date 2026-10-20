using UniCar.Api.Common;
using UniCar.Api.Users;
using Xunit;

namespace UniCar.Tests;

public class ValidationTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);

    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224725", true)]
    [InlineData("529.982.247-24", false)]
    [InlineData("111.111.111-11", false)]
    [InlineData("123", false)]
    [InlineData(null, false)]
    public void Cpf_confere_digitos(string? cpf, bool valido) => Assert.Equal(valido, Cpf.IsValid(cpf));

    [Fact]
    public void Cpf_mascarado_mostra_so_o_final() => Assert.Equal("***.***.*47-25", Cpf.Masked("52998224725"));

    [Theory]
    [InlineData("(41) 99999-0000", true)]
    [InlineData("41999990000", true)]
    [InlineData("(41) 3333-0000", false)]
    public void Celular_com_ddd(string phone, bool valido) => Assert.Equal(valido, Phone.IsValidMobile(phone));

    [Fact]
    public void Idade_conta_o_aniversario()
    {
        Assert.Equal(17, UserValidation.AgeOn(new DateOnly(2008, 10, 20), Today));
        Assert.Equal(18, UserValidation.AgeOn(new DateOnly(2008, 10, 19), Today));
    }

    [Fact]
    public void Menor_de_idade_nao_cadastra()
    {
        var erro = UserValidation.Validate("Ana Ribeiro", "2009-01-01", "52998224725", "41999990000", "Direito", 3, "Noite",
            Today, out _, out _);
        Assert.Equal("underage", erro?.Code);
    }

    [Fact]
    public void Cadastro_valido_passa()
    {
        var erro = UserValidation.Validate("Ana  Ribeiro", "2004-03-10", "529.982.247-25", "(41) 99999-0000", "Direito", 3, "noite",
            Today, out var nasc, out var turno);
        Assert.Null(erro);
        Assert.Equal(new DateOnly(2004, 3, 10), nasc);
        Assert.Equal(UniCar.Api.Domain.Shift.Noite, turno);
    }

    [Fact]
    public void Curso_fora_da_lista_e_recusado()
    {
        var erro = UserValidation.Validate("Ana Ribeiro", "2004-03-10", "52998224725", "41999990000", "Curso Inventado", 3, "Noite",
            Today, out _, out _);
        Assert.Equal("invalid_course", erro?.Code);
    }
}
