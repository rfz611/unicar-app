using Microsoft.Extensions.Options;

namespace UniCar.Api.Common;

/// <summary>
/// Arquivos públicos (fotos de perfil), servidos em /uploads/... pela própria API.
/// Em desenvolvimento ficam em wwwroot/uploads (fora do Git); depois, trocar por um storage (S3, Azure Blob).
/// </summary>
public class PublicStorage(IOptions<StorageOptions> opt, IWebHostEnvironment env)
{
    public const string RequestPath = "/uploads";

    public string Root { get; } = string.IsNullOrWhiteSpace(opt.Value.PublicRoot)
        ? Path.Combine(env.ContentRootPath, "wwwroot", "uploads")
        : opt.Value.PublicRoot;

    /// <summary>Grava e devolve a URL relativa ("/uploads/avatars/abc.jpg").</summary>
    public async Task<string> SaveAsync(string folder, string fileName, byte[] bytes, CancellationToken ct)
    {
        var dir = Path.Combine(Root, folder);
        Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(Path.Combine(dir, Path.GetFileName(fileName)), bytes, ct);
        return $"{RequestPath}/{folder}/{Path.GetFileName(fileName)}";
    }

    /// <summary>Apaga pelo URL devolvido em <see cref="SaveAsync"/>. Ignora URLs de fora.</summary>
    public void Delete(string? url)
    {
        if (url is null || !url.StartsWith(RequestPath + "/")) return;
        var parts = url[(RequestPath.Length + 1)..].Split('/');
        if (parts.Length != 2) return;
        var path = Path.Combine(Root, Path.GetFileName(parts[0]), Path.GetFileName(parts[1]));
        if (File.Exists(path)) File.Delete(path);
    }
}
