namespace UniCar.Api.Common;

public static class Phone
{
    /// <summary>Celular brasileiro com DDD: 11 dígitos, terceiro dígito 9.</summary>
    public static bool IsValidMobile(string? value)
    {
        var d = Cpf.Digits(value);
        return d.Length == 11 && d[2] == '9' && d[0] != '0';
    }

    public static string Format(string value)
    {
        var d = Cpf.Digits(value);
        return d.Length == 11 ? $"({d[..2]}) {d.Substring(2, 5)}-{d[7..]}" : value;
    }
}
