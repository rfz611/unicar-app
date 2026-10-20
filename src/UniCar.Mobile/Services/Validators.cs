using System.Text.RegularExpressions;

namespace UniCar.Mobile.Services;

public static partial class Validators
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public static bool IsValidEmail(string? email)
        => !string.IsNullOrWhiteSpace(email) && EmailRegex().IsMatch(email.Trim());

    /// <summary>Mesma regra da API (AuthController.Register).</summary>
    public static bool IsInstitutionalEmail(string? email)
    {
        if (!IsValidEmail(email)) return false;
        var e = email!.Trim().ToLowerInvariant();
        return e.EndsWith("@positivo.edu.br") || e.EndsWith("up.edu.br");
    }

    public static string OnlyDigits(string? value)
        => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool IsValidPhone(string? phone)
    {
        var digits = OnlyDigits(phone).Length;
        return digits is 10 or 11;
    }

    /// <summary>Máscara de telefone brasileiro: (41) 99999-8888.</summary>
    public static string FormatPhone(string? input)
    {
        var d = OnlyDigits(input);
        if (d.Length > 11) d = d[..11];

        return d.Length switch
        {
            0 => string.Empty,
            <= 2 => $"({d}",
            <= 6 => $"({d[..2]}) {d[2..]}",
            <= 10 => $"({d[..2]}) {d[2..6]}-{d[6..]}",
            _ => $"({d[..2]}) {d[2..7]}-{d[7..]}"
        };
    }

    // ---- CNH -------------------------------------------------------------

    /// <summary>Categorias que habilitam dirigir carro (A é só moto). Mesma regra da API.</summary>
    public static readonly string[] CarCnhCategories = { "B", "C", "D", "E", "AB", "AC", "AD", "AE" };

    /// <summary>Confere os 2 dígitos verificadores da CNH. Mesmo algoritmo da API.</summary>
    public static bool IsValidCnh(string? cnh)
    {
        var d = OnlyDigits(cnh);
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;

        var n = d.Select(c => c - '0').ToArray();

        var sum = 0;
        for (int i = 0, weight = 9; i < 9; i++, weight--) sum += n[i] * weight;
        var dv1 = sum % 11;
        var discount = 0;
        if (dv1 >= 10)
        {
            dv1 = 0;
            discount = 2;
        }

        sum = 0;
        for (int i = 0, weight = 1; i < 9; i++, weight++) sum += n[i] * weight;
        var x = sum % 11;
        var dv2 = x - discount < 0 ? x - discount + 11 : x - discount;
        if (dv2 >= 10) dv2 = 0;

        return n[9] == dv1 && n[10] == dv2;
    }

    public static bool IsCarCnhCategory(string? category)
        => CarCnhCategories.Contains((category ?? string.Empty).Trim().ToUpperInvariant());

    /// <summary>Máscara de data: 31/12/2030.</summary>
    public static string FormatDate(string? input)
    {
        var d = OnlyDigits(input);
        if (d.Length > 8) d = d[..8];

        return d.Length switch
        {
            <= 2 => d,
            <= 4 => $"{d[..2]}/{d[2..]}",
            _ => $"{d[..2]}/{d[2..4]}/{d[4..]}"
        };
    }

    public static bool TryParseDate(string? text, out DateOnly date)
        => DateOnly.TryParseExact(text?.Trim(), "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out date);
}
