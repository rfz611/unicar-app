namespace UniCar.Api.Common;

public static class Cpf
{
    /// <summary>Mantém só os dígitos.</summary>
    public static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>Confere os dois dígitos verificadores oficiais do CPF.</summary>
    public static bool IsValid(string? value)
    {
        var d = Digits(value);
        if (d.Length != 11) return false;
        if (d.All(c => c == d[0])) return false;

        int Check(int length)
        {
            var sum = 0;
            for (var i = 0; i < length; i++) sum += (d[i] - '0') * (length + 1 - i);
            var r = sum * 10 % 11;
            return r == 10 ? 0 : r;
        }

        return Check(9) == d[9] - '0' && Check(10) == d[10] - '0';
    }

    /// <summary>Mostra só o final: ***.***.*47-25.</summary>
    public static string Masked(string digits) =>
        digits.Length == 11 ? $"***.***.*{digits[7]}{digits[8]}-{digits[9]}{digits[10]}" : "***";
}
