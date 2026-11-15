using SkiaSharp;

namespace Drawing.API.Infrastructure;

public sealed class SafeImageService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private const long MaxBytes = 10 * 1024 * 1024;
    public string StorageRoot => RootPath;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public SafeImageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    private string RootPath
    {
        get
        {
            var configured = _configuration["Media:RootPath"];
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(_environment.ContentRootPath, "uploads")
                : configured;
        }
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct, string folder = "artworks")
    {
        if (file is null || file.Length <= 0 || file.Length > MaxBytes)
            throw new ArgumentException("Şəkil maksimum 10 MB olmalıdır.");

        if (folder is not ("artworks" or "slides" or "profiles"))
            throw new ArgumentException("Yanlış qovluq.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!Extensions.Contains(extension))
            throw new ArgumentException("Yalnız JPG, PNG və WEBP qəbul olunur.");

        await using var memory = new MemoryStream((int)file.Length);

        await file.CopyToAsync(memory, ct);

        var bytes = memory.ToArray();

        using var imageData = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(imageData);

        if (codec is null)
            throw new ArgumentException("Fayl düzgün şəkil deyil.");

        var info = codec.Info;
        var pixels = (long)info.Width * info.Height;

        if (info.Width <= 0 || info.Height <= 0 || info.Width > 10000 || info.Height > 10000 || pixels > 16000000)
            throw new ArgumentException("Şəklin ölçüləri icazə verilən həddi keçir.");

        using var bitmap = SKBitmap.Decode(bytes);

        if (bitmap is null)
            throw new ArgumentException("Şəkil oxuna bilmir.");

        var expectedFormat = extension switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };

        if (codec.EncodedFormat != expectedFormat)
            throw new ArgumentException("Fayl uzantısı şəkil formatına uyğun deyil.");

        var name = Guid.NewGuid().ToString("N");
        var key = $"{folder}/{name}{extension}";
        var path = ResolvePath(key);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string? thumbnailPath = null;

        try
        {
            await File.WriteAllBytesAsync(path, bytes, ct);

            if (folder == "artworks")
            {
                var width = Math.Min(bitmap.Width, 720);
                var height = Math.Max(1, (int)Math.Round(bitmap.Height * (double)width / bitmap.Width));

                using var thumbnail = new SKBitmap(width, height);
                using var canvas = new SKCanvas(thumbnail);

                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(bitmap, new SKRect(0, 0, width, height));
                canvas.Flush();

                using var image = SKImage.FromBitmap(thumbnail);
                using var webp = image.Encode(SKEncodedImageFormat.Webp, 85);

                if (webp is null) throw new InvalidOperationException("Kiçik şəkil yaradıla bilmədi.");

                thumbnailPath = ResolvePath($"thumbnails/{name}.webp");

                Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

                await File.WriteAllBytesAsync(thumbnailPath, webp.ToArray(), ct);
            }

            return key;
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            if (thumbnailPath is not null && File.Exists(thumbnailPath)) File.Delete(thumbnailPath);
            throw;
        }
    }

    public string ResolvePath(string key)
    {
        var parts = key.Split('/');

        if (parts.Length != 2)
            throw new ArgumentException("Yanlış fayl ünvanı.");

        if (parts[0] is not ("artworks" or "slides" or "profiles" or "thumbnails"))
            throw new ArgumentException("Yanlış fayl qovluğu.");

        if (string.IsNullOrWhiteSpace(parts[1]) || parts[1] != Path.GetFileName(parts[1]))
            throw new ArgumentException("Yanlış fayl adı.");

        var extension = Path.GetExtension(parts[1]);

        if (!Extensions.Contains(extension))
            throw new ArgumentException("Yanlış fayl formatı.");

        return Path.Combine(RootPath, parts[0], parts[1]);
    }

    public void DeleteFiles(string key)
    {
        var path = ResolvePath(key);

        if (File.Exists(path)) File.Delete(path);

        if (key.StartsWith("artworks/", StringComparison.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(key);
            var thumbnailPath = ResolvePath($"thumbnails/{name}.webp");

            if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath);
        }
    }
}
