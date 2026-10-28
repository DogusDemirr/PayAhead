using PayAhead.Application.Auth;
using SkiaSharp;

namespace PayAhead.Infrastructure;
public sealed class AvatarImageProcessor : IAvatarImageProcessor
{
    public byte[] Normalize(byte[] input)
    {
        if (input.Length > AvatarService.MaximumBytes) throw new AvatarException("avatar_too_large");
        using var stream = new MemoryStream(input, false);
        using var codec = SKCodec.Create(stream);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp)) throw new AvatarException("avatar_invalid");
        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || codec.FrameCount > 1) throw new AvatarException("avatar_invalid");
        if (codec.Info.Width > 2048 || codec.Info.Height > 2048) throw new AvatarException("avatar_too_large");
        using var bitmap = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) throw new AvatarException("avatar_invalid");
        using var source = SKImage.FromBitmap(bitmap);
        using var target = SKSurface.Create(new SKImageInfo(512, 512));
        target.Canvas.Clear(SKColors.White);
        var size = Math.Min(bitmap.Width, bitmap.Height); var left = (bitmap.Width - size) / 2f; var top = (bitmap.Height - size) / 2f;
        target.Canvas.DrawImage(source, new SKRect(left, top, left + size, top + size), new SKRect(0, 0, 512, 512), new SKSamplingOptions(SKFilterMode.Linear), null);
        // New pixel-only JPEG: source EXIF, GPS, filenames and embedded profiles are not copied.
        using var result = target.Snapshot(); using var encoded = result.Encode(SKEncodedImageFormat.Jpeg, 85);
        return encoded.ToArray();
    }
}
