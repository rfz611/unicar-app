using System.ComponentModel.DataAnnotations;

namespace UniCar.API.DTOs;

public class RegisterRequestDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string RaEnrollment { get; set; } = string.Empty;

    /// <summary>PASSENGER, DRIVER ou BOTH.</summary>
    public string UserType { get; set; } = "BOTH";

    // Obrigatórios quando UserType for DRIVER ou BOTH
    public string? CnhNumber { get; set; }

    /// <summary>B, C, D, E, AB, AC, AD ou AE.</summary>
    public string? CnhCategory { get; set; }

    /// <summary>Validade da CNH no formato yyyy-MM-dd.</summary>
    public DateOnly? CnhExpiry { get; set; }
}

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
