namespace UniCar.Mobile.Models;

/// <summary>Tipos de usuário aceitos pela API (coluna user_type do banco).</summary>
public static class UserTypes
{
    public const string Passenger = "PASSENGER";
    public const string Driver = "DRIVER";
    public const string Both = "BOTH";

    public static string Describe(string? userType) => userType switch
    {
        Passenger => "Passageiro",
        Driver => "Motorista",
        _ => "Passageiro e motorista"
    };
}

// Espelham os DTOs da API (UniCar.API/DTOs/AuthDTOs.cs)
public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string Phone,
    string RaEnrollment,
    string UserType,
    string? CnhNumber = null,
    string? CnhCategory = null,
    string? CnhExpiry = null);   // yyyy-MM-dd

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Code, string NewPassword);

/// <summary>Resposta de forgot-password / reset-password. DevCode só vem preenchido com a API em Development.</summary>
public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
    public string? DevCode { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
