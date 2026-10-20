using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace UniCar.Api.Common;

/// <summary>
/// Protege dados pessoais sensíveis (LGPD): criptografa para guardar e gera um hash
/// com segredo (HMAC) para checar duplicidade sem guardar o valor aberto.
/// </summary>
public class SensitiveData(IDataProtectionProvider provider, IConfiguration config)
{
    private readonly IDataProtector _protector = provider.CreateProtector("UniCar.SensitiveData.v1");
    private readonly byte[] _pepper = Encoding.UTF8.GetBytes(
        config["Security:HashPepper"] ?? throw new InvalidOperationException("Configure Security:HashPepper."));

    public string Encrypt(string plain) => _protector.Protect(plain);
    public string Decrypt(string cipher) => _protector.Unprotect(cipher);

    public string Hash(string value)
    {
        using var hmac = new HMACSHA256(_pepper);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }
}
