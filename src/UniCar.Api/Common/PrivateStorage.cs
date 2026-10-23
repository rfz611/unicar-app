using Microsoft.Extensions.Options;

namespace UniCar.Api.Common;

public class StorageOptions
{
    public const string Section = "Storage";
    /// <summary>Pasta dos documentos (CNH, selfie). Fica fora do wwwroot: só sai pela API, para admin. Vazio = App_Data/private.</summary>
    public string PrivateRoot { get; set; } = string.Empty;
    /// <summary>Pasta das fotos de perfil (servidas em /uploads). Vazio = wwwroot/uploads.</summary>
    public string PublicRoot { get; set; } = string.Empty;
}

/// <summary>Imagem enviada pelo app: confere tamanho e o tipo real pelos primeiros bytes (não confia na extensão).</summary>
public static class ImageUpload
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public static ApiError? Check(IFormFile? file, out string extension)
    {
        extension = string.Empty;
        if (file is null || file.Length == 0) return new ApiError("missing_file", "Escolha uma foto.");
        if (file.Length > MaxBytes) return new ApiError("file_too_large", "A foto pode ter no máximo 5 MB.");

        Span<byte> head = stackalloc byte[12];
        using (var s = file.OpenReadStream())
        {
            var read = s.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
            head = head[..read];
        }
        extension = Sniff(head);
        return extension.Length == 0 ? new ApiError("invalid_image", "A foto precisa ser JPG, PNG ou WebP.") : null;
    }

    private static string Sniff(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
        if (h.Length >= 8 && h[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8)) return ".webp";
        return string.Empty;
    }

    public static string ContentType(string fileName) => Path.GetExtension(fileName) switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
}

/// <summary>Arquivos privados por usuário: {raiz}/{userId}/{nome}.</summary>
public class PrivateStorage(IOptions<StorageOptions> opt, IWebHostEnvironment env)
{
    private readonly string _root = string.IsNullOrWhiteSpace(opt.Value.PrivateRoot)
        ? Path.Combine(env.ContentRootPath, "App_Data", "private")
        : opt.Value.PrivateRoot;

    /// <summary>Grava e devolve o nome do arquivo (ex.: "cnh-3f2a.jpg"). Apaga a versão anterior, se houver.</summary>
    public async Task<string> SaveAsync(Guid userId, string prefix, string extension, IFormFile file, string? previous,
        CancellationToken ct)
    {
        var dir = Path.Combine(_root, userId.ToString());
        Directory.CreateDirectory(dir);
        var name = $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 9)] + extension;
        await using (var fs = File.Create(Path.Combine(dir, name)))
            await file.CopyToAsync(fs, ct);
        if (previous is not null) Delete(userId, previous);
        return name;
    }

    /// <summary>Grava bytes prontos (usado pelo seed de demonstração).</summary>
    public async Task<string> SaveBytesAsync(Guid userId, string prefix, string extension, byte[] bytes, CancellationToken ct)
    {
        var dir = Path.Combine(_root, userId.ToString());
        Directory.CreateDirectory(dir);
        var name = $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 9)] + extension;
        await File.WriteAllBytesAsync(Path.Combine(dir, name), bytes, ct);
        return name;
    }

    public Stream? Open(Guid userId, string name)
    {
        var path = Path.Combine(_root, userId.ToString(), Path.GetFileName(name));
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public void DeleteUserFolder(Guid userId)
    {
        var dir = Path.Combine(_root, userId.ToString());
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    public void Delete(Guid userId, string name)
    {
        var path = Path.Combine(_root, userId.ToString(), Path.GetFileName(name));
        if (File.Exists(path)) File.Delete(path);
    }
}
