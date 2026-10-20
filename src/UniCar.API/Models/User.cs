using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniCar.API.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("phone")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Column("ra_enrollment")]
    [MaxLength(30)]
    public string RaEnrollment { get; set; } = string.Empty;

    [Column("user_type")]
    public string UserType { get; set; } = "BOTH";

    [Column("profile_photo_url")]
    public string? ProfilePhotoUrl { get; set; }

    // --- CNH (obrigatória para DRIVER e BOTH) ---
    [Column("cnh_number")]
    [MaxLength(11)]
    public string? CnhNumber { get; set; }

    [Column("cnh_category")]
    [MaxLength(2)]
    public string? CnhCategory { get; set; }

    [Column("cnh_expiry")]
    public DateOnly? CnhExpiry { get; set; }

    // --- Redefinição de senha (código de 6 dígitos guardado como hash) ---
    [Column("password_reset_code_hash")]
    [MaxLength(255)]
    public string? PasswordResetCodeHash { get; set; }

    [Column("password_reset_expires_at")]
    public DateTime? PasswordResetExpiresAt { get; set; }

    [Column("password_reset_attempts")]
    public int PasswordResetAttempts { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
