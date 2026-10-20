namespace UniCar.Api.Auth;

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "unicar-api";
    public string Audience { get; set; } = "unicar-app";
    /// <summary>Chave secreta com pelo menos 32 caracteres. Em produção, fica fora do código.</summary>
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenDays { get; set; } = 30;
    public int SignupTokenMinutes { get; set; } = 30;
}

public class EmailCodeOptions
{
    public const string Section = "EmailCode";
    public int ExpiresMinutes { get; set; } = 10;
    public int MaxAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxCodesPerHour { get; set; } = 5;
}

public static class TokenPurpose
{
    public const string Claim = "purpose";
    public const string Access = "access";
    public const string Signup = "signup";
}
