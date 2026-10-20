namespace UniCar.Api.Auth;

public record RequestCodeRequest(string Email);
public record RequestCodeResponse(string University, int ExpiresInMinutes);

public record VerifyCodeRequest(string Email, string Code);

/// <summary>
/// Se a pessoa já tem conta, vem <see cref="AccessToken"/>.
/// Se é a primeira vez, vem <see cref="SignupToken"/> para enviar em /api/auth/register.
/// </summary>
public record VerifyCodeResponse(bool IsNewUser, string? AccessToken, string? SignupToken);

public record RegisterRequest(
    string FullName,
    string BirthDate,   // "AAAA-MM-DD"
    string Cpf,
    string Phone,
    string Course,
    int Period,
    string Shift);      // "Manha" | "Tarde" | "Noite"

public record AuthResponse(string AccessToken, Users.UserDto User);
