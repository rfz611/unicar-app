namespace UniCar.Api.Domain;

/// <summary>
/// Pedido para virar motorista. Um por pessoa: reenviar substitui os dados.
/// Só vai para análise (User.DriverStatus = InReview) quando tem dados + foto da CNH + selfie.
/// </summary>
public class DriverApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Número da CNH criptografado (LGPD), como o CPF.</summary>
    public required string CnhEncrypted { get; set; }
    /// <summary>Hash para impedir a mesma CNH em duas contas.</summary>
    public required string CnhHash { get; set; }
    /// <summary>Último dia do mês de validade informado (MM/AAAA).</summary>
    public DateOnly CnhExpiresOn { get; set; }

    public required string Plate { get; set; }
    public required string VehicleModel { get; set; }
    public required string VehicleColor { get; set; }

    /// <summary>Nome do arquivo no armazenamento privado (nunca em wwwroot).</summary>
    public string? CnhPhotoFile { get; set; }
    public string? SelfieFile { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public string? RejectionReason { get; set; }

    public bool IsComplete => CnhPhotoFile is not null && SelfieFile is not null;
}

/// <summary>Carro do motorista. Criado quando o admin aprova o pedido.</summary>
public class Vehicle
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Placa sem traço, maiúscula: ABC1234 ou ABC1D23.</summary>
    public required string Plate { get; set; }
    public required string Model { get; set; }
    public required string Color { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
