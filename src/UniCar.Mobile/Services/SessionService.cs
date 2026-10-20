using System.Text;
using System.Text.Json;
using UniCar.Mobile.Models;

namespace UniCar.Mobile.Services;

/// <summary>Guarda a sessão do usuário logado (token no armazenamento seguro; perfil nas preferências).</summary>
public class SessionService
{
    private const string TokenKey = "unicar_token";
    private const string LoggedKey = "unicar_logged";
    private const string NameKey = "unicar_name";
    private const string EmailKey = "unicar_email";
    private const string RoleKey = "unicar_role";

    public bool IsLoggedIn => Preferences.Get(LoggedKey, false);
    public string Name => Preferences.Get(NameKey, string.Empty);
    public string Email => Preferences.Get(EmailKey, string.Empty);
    public string Role => Preferences.Get(RoleKey, UserTypes.Both);

    public async Task SaveAsync(AuthResponse auth, string? role)
    {
        try
        {
            await SecureStorage.Default.SetAsync(TokenKey, auth.Token);
        }
        catch
        {
            // SecureStorage pode não estar disponível (ex.: simulador sem keychain).
            Preferences.Set(TokenKey, auth.Token);
        }

        Preferences.Set(NameKey, auth.Name);
        Preferences.Set(EmailKey, auth.Email);
        Preferences.Set(RoleKey, role ?? ReadRoleFromToken(auth.Token) ?? UserTypes.Both);
        Preferences.Set(LoggedKey, true);
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(TokenKey) ?? Preferences.Get(TokenKey, null);
        }
        catch
        {
            return Preferences.Get(TokenKey, null);
        }
    }

    public void Clear()
    {
        try { SecureStorage.Default.Remove(TokenKey); } catch { /* ignora */ }
        Preferences.Remove(TokenKey);
        Preferences.Remove(NameKey);
        Preferences.Remove(EmailKey);
        Preferences.Remove(RoleKey);
        Preferences.Set(LoggedKey, false);
    }

    /// <summary>Lê o claim "role" (PASSENGER/DRIVER/BOTH) do JWT emitido pela API.</summary>
    public static string? ReadRoleFromToken(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return doc.RootElement.TryGetProperty("role", out var role) ? role.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
