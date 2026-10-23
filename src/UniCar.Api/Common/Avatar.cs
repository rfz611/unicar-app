using SkiaSharp;

namespace UniCar.Api.Common;

/// <summary>
/// Foto de perfil: corta no centro em quadrado, reduz para 512×512 e salva como JPEG.
/// Recodificar tira os metadados (EXIF), que podem ter a localização de onde a foto foi tirada.
/// </summary>
public static class Avatar
{
    public const int Size = 512;
    /// <summary>Recusa imagens gigantes (proteção contra "bomba de descompressão").</summary>
    public const long MaxSourcePixels = 40_000_000;

    /// <summary>JPEG pronto, ou null se não for uma imagem que dá para abrir.</summary>
    public static byte[]? Make(byte[] input)
    {
        using var data = SKData.CreateCopy(input);
        using var codec = SKCodec.Create(data);
        if (codec is null) return null;
        if ((long)codec.Info.Width * codec.Info.Height > MaxSourcePixels) return null;

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null) return null;
        using var oriented = Orient(decoded, codec.EncodedOrigin);

        var side = Math.Min(oriented.Width, oriented.Height);
        var source = SKRect.Create((oriented.Width - side) / 2f, (oriented.Height - side) / 2f, side, side);
        var target = Math.Min(Size, side);

        using var surface = SKSurface.Create(new SKImageInfo(target, target, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.White); // PNG transparente fica com fundo branco no JPEG
        using var image = SKImage.FromBitmap(oriented);
        surface.Canvas.DrawImage(image, source, SKRect.Create(target, target), new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var snapshot = surface.Snapshot();
        using var jpeg = snapshot.Encode(SKEncodedImageFormat.Jpeg, 85);
        return jpeg.ToArray();
    }

    /// <summary>Foto de celular "deitada": aplica a rotação que vem no EXIF antes de descartá-lo.</summary>
    private static SKBitmap Orient(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        var degrees = origin switch
        {
            SKEncodedOrigin.BottomRight => 180,
            SKEncodedOrigin.RightTop => 90,
            SKEncodedOrigin.LeftBottom => 270,
            _ => 0
        };
        if (degrees == 0) return bitmap.Copy();

        var swap = degrees != 180;
        var rotated = new SKBitmap(swap ? bitmap.Height : bitmap.Width, swap ? bitmap.Width : bitmap.Height);
        using var canvas = new SKCanvas(rotated);
        canvas.Translate(rotated.Width / 2f, rotated.Height / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
        canvas.DrawBitmap(bitmap, 0, 0);
        return rotated;
    }
}
