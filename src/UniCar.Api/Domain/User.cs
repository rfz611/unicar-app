namespace UniCar.Api.Domain;

public enum Shift { Manha, Tarde, Noite }

public enum DriverStatus { None, InReview, Approved, Rejected }

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>E-mail institucional já verificado, sempre em minúsculas.</summary>
    public required string Email { get; set; }
    public int UniversityId { get; set; }
    public University? University { get; set; }
    /// <summary>Campus escolhido para o atalho "Minha faculdade". Vazio = primeiro campus da faculdade.</summary>
    public int? CampusId { get; set; }
    public Campus? Campus { get; set; }

    public required string FullName { get; set; }
    public DateOnly BirthDate { get; set; }
    public required string Phone { get; set; }

    /// <summary>CPF criptografado (LGPD). Nunca é devolvido inteiro pela API.</summary>
    public required string CpfEncrypted { get; set; }
    /// <summary>Hash do CPF para impedir duas contas com o mesmo CPF sem guardar o número aberto.</summary>
    public required string CpfHash { get; set; }

    public required string Course { get; set; }
    public int Period { get; set; }
    public Shift Shift { get; set; }
    public string? Gender { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }

    public string? TrustedContactPhone { get; set; }
    public DriverStatus DriverStatus { get; set; } = DriverStatus.None;

    public DateTimeOffset EmailVerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<SavedPlace> SavedPlaces { get; set; } = new();
    public UserPreferences? Preferences { get; set; }

    /// <summary>O campus do atalho "Minha faculdade": o escolhido ou o primeiro da faculdade.</summary>
    public Campus? MainCampus => Campus ?? University?.Campuses.OrderBy(c => c.Id).FirstOrDefault();
}

public enum Theme { System, Light, Dark }

/// <summary>Tela de Configurações. Uma linha por usuário, criada no cadastro.</summary>
public class UserPreferences
{
    public Guid UserId { get; set; }
    public Theme Theme { get; set; } = Theme.System;
    /// <summary>Avisos de carona (nova carona para o destino, mudança de horário etc.).</summary>
    public bool RideAlerts { get; set; } = true;
    public bool AlwaysShareTrip { get; set; }
    /// <summary>Outros universitários veem o curso no perfil público.</summary>
    public bool ShowCourse { get; set; } = true;
}

public enum SavedPlaceKind { Home, Work, Other }

/// <summary>Casa, trabalho etc. O endereço completo fica só com o dono; os outros veem o bairro.</summary>
public class SavedPlace
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public SavedPlaceKind Kind { get; set; }
    public required string Label { get; set; }
    public required string Address { get; set; }
    public string? Neighborhood { get; set; }
    public string? GooglePlaceId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
