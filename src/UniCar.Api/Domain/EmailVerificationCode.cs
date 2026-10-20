namespace UniCar.Api.Domain;

/// <summary>Código de 6 dígitos enviado ao e-mail da faculdade. Guardamos só o hash.</summary>
public class EmailVerificationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string CodeHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
