namespace UniCar.API.Services;

/// <summary>
/// Valida os dados da CNH informados no cadastro de motoristas.
/// Confere formato, dígitos verificadores, categoria e validade.
/// Não consulta o Detran/Senatran: não prova que a CNH pertence à pessoa nem que está ativa.
/// </summary>
public static class CnhValidator
{
    // Categorias que habilitam dirigir carro (A é só moto).
    private static readonly HashSet<string> CarCategories = new() { "B", "C", "D", "E", "AB", "AC", "AD", "AE" };

    public static string OnlyDigits(string? value)
        => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

    public static string NormalizeCategory(string? category)
        => (category ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>Confere os 2 dígitos verificadores do número de registro (11 dígitos).</summary>
    public static bool IsValidNumber(string? cnh)
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

    /// <summary>Retorna a mensagem de erro, ou null quando os dados da CNH são aceitos.</summary>
    public static string? Validate(string? number, string? category, DateOnly? expiry, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(number))
            return "Informe o número da CNH.";

        if (!IsValidNumber(number))
            return "Número da CNH inválido.";

        var cat = NormalizeCategory(category);
        if (cat == "A")
            return "A categoria A habilita apenas motos. Para oferecer caronas de carro é necessário ter categoria B ou superior.";
        if (!CarCategories.Contains(cat))
            return "Categoria da CNH inválida. Use B, C, D, E, AB, AC, AD ou AE.";

        if (expiry is null)
            return "Informe a validade da CNH.";
        if (expiry.Value < today)
            return "CNH vencida. Renove sua habilitação para oferecer caronas.";
        if (expiry.Value > today.AddYears(11))
            return "Validade da CNH inválida.";

        return null;
    }
}
