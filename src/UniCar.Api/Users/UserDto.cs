using UniCar.Api.Common;
using UniCar.Api.Domain;

namespace UniCar.Api.Users;

/// <summary>O que o próprio usuário vê do perfil dele. CPF só aparece mascarado.</summary>
public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string University,
    string BirthDate,
    string Phone,
    string CpfMasked,
    string Course,
    int Period,
    string Shift,
    string? Gender,
    string? Bio,
    string? PhotoUrl,
    string? TrustedContactPhone,
    string DriverStatus,
    DateTimeOffset EmailVerifiedAt)
{
    public static UserDto From(User u, string cpfDigits) => new(
        u.Id, u.Email, u.FullName, u.University?.ShortName ?? string.Empty,
        u.BirthDate.ToString("yyyy-MM-dd"), u.Phone, Cpf.Masked(cpfDigits),
        u.Course, u.Period, u.Shift.ToString(), u.Gender, u.Bio, u.PhotoUrl,
        u.TrustedContactPhone, u.DriverStatus.ToString(), u.EmailVerifiedAt);
}

/// <summary>O que outros universitários veem: sem CPF, celular, e-mail ou data de nascimento.</summary>
public record PublicUserDto(Guid Id, string FirstName, string University, string? Course, int Period, string? Bio, string? PhotoUrl)
{
    public static PublicUserDto From(User u) => new(
        u.Id, u.FullName.Split(' ')[0], u.University?.ShortName ?? string.Empty, u.Course, u.Period, u.Bio, u.PhotoUrl);
}

public record UpdateMeRequest(
    string FullName, string BirthDate, string Phone, string Course, int Period, string Shift,
    string? Gender, string? Bio, string? TrustedContactPhone);
