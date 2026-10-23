using System.Globalization;

namespace UniCar.Api.Common;

public static class Cnh
{
    /// <summary>
    /// Confere os dois dígitos verificadores do número de registro da CNH (11 dígitos), pelo
    /// algoritmo usado pelo Denatran: pesos 9..1 no primeiro e 1..9 no segundo, com o "desconto"
    /// de 2 no segundo dígito quando o primeiro dá 10 ou mais.
    /// </summary>
    public static bool IsValid(string? value)
    {
        var d = Cpf.Digits(value);
        if (d.Length != 11 || d.All(c => c == d[0])) return false;

        var sum = 0;
        for (var i = 0; i < 9; i++) sum += (d[i] - '0') * (9 - i);
        var dv1 = sum % 11;
        var discount = 0;
        if (dv1 >= 10) { dv1 = 0; discount = 2; }

        sum = 0;
        for (var i = 0; i < 9; i++) sum += (d[i] - '0') * (i + 1);
        var dv2 = sum % 11;
        dv2 = dv2 >= 10 ? 0 : dv2 - discount;
        if (dv2 < 0) dv2 += 11;
        if (dv2 >= 10) dv2 = 0;

        return dv1 == d[9] - '0' && dv2 == d[10] - '0';
    }

    /// <summary>Mostra só os 4 últimos: *******7637.</summary>
    public static string Masked(string digits) => digits.Length == 11 ? new string('*', 7) + digits[7..] : "***";

    /// <summary>"MM/AAAA" → último dia daquele mês (a CNH vale até o fim do mês).</summary>
    public static bool TryParseExpiry(string? value, out DateOnly lastDay)
    {
        lastDay = default;
        if (!DateOnly.TryParseExact(value?.Trim(), "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
            return false;
        lastDay = first.AddMonths(1).AddDays(-1);
        return true;
    }
}
