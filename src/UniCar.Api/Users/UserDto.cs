using UniCar.Api.Common;
using UniCar.Api.Domain;

namespace UniCar.Api.Users;

public record CampusDto(int Id, string Name, string Address, double Latitude, double Longitude)
{
    public static CampusDto? From(Campus? c) => c is null ? null : new(c.Id, c.Name, c.Address, c.Latitude, c.Longitude);
}

/// <summary>
/// O que o próprio usuário vê do perfil dele. CPF só aparece mascarado.
/// <see cref="Campus"/> é o atalho "Minha faculdade"; <see cref="CanChooseCampus"/> diz se a faculdade tem mais de um.
/// Para montar, carregue University.Campuses, Campus e Preferences.
/// </summary>
public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string University,
    CampusDto? Campus,
    bool CanChooseCampus,
    string BirthDate,
    string Phone,
    string CpfMasked,
    string Course,
    bool ShowCourse,
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
        CampusDto.From(u.MainCampus), (u.University?.Campuses.Count ?? 0) > 1,
        u.BirthDate.ToString("yyyy-MM-dd"), u.Phone, Cpf.Masked(cpfDigits),
        u.Course, u.Preferences?.ShowCourse ?? true, u.Period, u.Shift.ToString(), u.Gender, u.Bio, u.PhotoUrl,
        u.TrustedContactPhone, u.DriverStatus.ToString(), u.EmailVerifiedAt);
}

/// <summary>O que outros universitários veem: sem CPF, celular, e-mail ou data de nascimento. Curso só se a pessoa permitir.</summary>
public record PublicUserDto(Guid Id, string FirstName, string University, string? Course, int Period, string? Bio, string? PhotoUrl)
{
    public static PublicUserDto From(User u) => new(
        u.Id, u.FullName.Split(' ')[0], u.University?.ShortName ?? string.Empty,
        u.Preferences?.ShowCourse == false ? null : u.Course, u.Period, u.Bio, u.PhotoUrl);
}

public record UpdateMeRequest(
    string FullName, string BirthDate, string Phone, string Course, int Period, string Shift,
    string? Gender, string? Bio, string? TrustedContactPhone, bool? ShowCourse = null);

public record ChooseCampusRequest(int CampusId);

public record PreferencesDto(string Theme, bool RideAlerts, bool AlwaysShareTrip, bool ShowCourse)
{
    public static PreferencesDto From(UserPreferences p) => new(p.Theme.ToString(), p.RideAlerts, p.AlwaysShareTrip, p.ShowCourse);
}
