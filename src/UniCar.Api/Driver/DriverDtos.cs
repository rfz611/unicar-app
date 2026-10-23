using UniCar.Api.Common;
using UniCar.Api.Domain;

namespace UniCar.Api.Driver;

/// <summary>Dados da tela "Virar motorista". cnhExpiry no formato "MM/AAAA".</summary>
public record DriverApplicationRequest(string Cnh, string CnhExpiry, string Plate, string Model, string Color);

/// <summary>
/// Situação do pedido. <see cref="Missing"/> diz o que falta para ir para análise ("cnhPhoto", "selfie").
/// <see cref="Status"/> é o mesmo do User.DriverStatus: None (incompleto), InReview, Approved, Rejected.
/// </summary>
public record DriverApplicationDto(
    string Status, string CnhMasked, string CnhExpiry, string Plate, string Model, string Color,
    bool HasCnhPhoto, bool HasSelfie, IReadOnlyList<string> Missing,
    DateTimeOffset? SubmittedAt, DateTimeOffset? ReviewedAt, string? RejectionReason)
{
    public static DriverApplicationDto From(DriverApplication a, DriverStatus status, string cnhDigits) => new(
        status.ToString(), Cnh.Masked(cnhDigits), a.CnhExpiresOn.ToString("MM/yyyy"), Common.Plate.Display(a.Plate),
        a.VehicleModel, a.VehicleColor, a.CnhPhotoFile is not null, a.SelfieFile is not null, MissingOf(a),
        a.SubmittedAt, a.ReviewedAt, a.RejectionReason);

    public static IReadOnlyList<string> MissingOf(DriverApplication a)
    {
        var missing = new List<string>();
        if (a.CnhPhotoFile is null) missing.Add("cnhPhoto");
        if (a.SelfieFile is null) missing.Add("selfie");
        return missing;
    }
}

public record VehicleDto(int Id, string Plate, string Model, string Color)
{
    public static VehicleDto From(Vehicle v) => new(v.Id, Common.Plate.Display(v.Plate), v.Model, v.Color);
}

/// <summary>Lista do painel de análise. Admin vê a CNH inteira para comparar com a foto.</summary>
public record AdminDriverDto(
    Guid UserId, string FullName, string Email, string University, string Status,
    string Cnh, string CnhExpiry, string Plate, string Model, string Color,
    bool HasCnhPhoto, bool HasSelfie, DateTimeOffset? SubmittedAt, string? RejectionReason);

public record RejectRequest(string Reason);
