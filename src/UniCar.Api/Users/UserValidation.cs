using System.Globalization;
using UniCar.Api.Common;
using UniCar.Api.Domain;

namespace UniCar.Api.Users;

public static class UserValidation
{
    public const int MinAge = 18;

    public static string CleanName(string? name) =>
        string.Join(' ', (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static int AgeOn(DateOnly birth, DateOnly today)
    {
        var age = today.Year - birth.Year;
        if (today < birth.AddYears(age)) age--;
        return age;
    }

    /// <summary>Devolve null se tudo estiver certo; senão, o primeiro erro com mensagem pronta para o usuário.</summary>
    public static ApiError? Validate(string? fullName, string? birthDate, string? cpf, string? phone, string? course,
        int period, string? shift, DateOnly today, out DateOnly birth, out Shift parsedShift)
    {
        birth = default;
        parsedShift = default;

        var name = CleanName(fullName);
        if (name.Split(' ').Length < 2 || name.Length < 5 || name.Length > 120)
            return new ApiError("invalid_name", "Escreva nome e sobrenome.");
        if (name.Any(c => char.IsDigit(c)))
            return new ApiError("invalid_name", "O nome não pode ter números.");

        if (!DateOnly.TryParseExact(birthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out birth))
            return new ApiError("invalid_birth", "Data de nascimento inválida.");
        var age = AgeOn(birth, today);
        if (age < MinAge) return new ApiError("underage", "O UniCar é para maiores de 18 anos.");
        if (age > 99) return new ApiError("invalid_birth", "Data de nascimento inválida.");

        if (!Phone.IsValidMobile(phone)) return new ApiError("invalid_phone", "Celular com DDD, 11 números.");
        if (!Cpf.IsValid(cpf)) return new ApiError("invalid_cpf", "CPF inválido. Confira os números.");
        if (!Courses.Exists(course)) return new ApiError("invalid_course", "Escolha um curso da lista.");
        if (period is < 1 or > 12) return new ApiError("invalid_period", "Período entre 1 e 12.");
        if (!Enum.TryParse(shift, ignoreCase: true, out parsedShift) || !Enum.IsDefined(parsedShift))
            return new ApiError("invalid_shift", "Escolha o turno.");

        return null;
    }
}
