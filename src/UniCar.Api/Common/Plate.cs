using System.Text.RegularExpressions;

namespace UniCar.Api.Common;

public static partial class Plate
{
    /// <summary>Tira traço e espaço e põe em maiúsculas: "abc-1d23" → "ABC1D23".</summary>
    public static string Normalize(string? value) =>
        new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    /// <summary>Placa antiga (ABC1234) ou Mercosul (ABC1D23).</summary>
    public static bool IsValid(string? value) => PlateRegex().IsMatch(Normalize(value));

    /// <summary>Formato de exibição: ABC-1234 / ABC1D23.</summary>
    public static string Display(string plate) => char.IsDigit(plate[4]) ? $"{plate[..3]}-{plate[3..]}" : plate;

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")]
    private static partial Regex PlateRegex();
}
