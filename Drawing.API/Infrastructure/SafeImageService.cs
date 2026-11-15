namespace Drawing.API.Infrastructure;
public sealed class SafeImageService
{
    private readonly IWebHostEnvironment _environment;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 10 * 1024 * 1024;

    public SafeImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct, string folder = "artworks")
    {
        if (file.Length <= 0 || file.Length > MaxBytes) throw new ArgumentException("Şəkil 10 MB-dan böyük ola bilməz.");
        if (folder is not ("artworks" or "slides")) throw new ArgumentException("Yanlış şəkil qovluğu.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension)) throw new ArgumentException("Yalnız JPG, PNG və WEBP qəbul olunur.");

        using var stream = file.OpenReadStream();
        var signature = new byte[12];
        var bytesRead = await stream.ReadAsync(signature.AsMemory(0, 12), ct);

        var isJpg = bytesRead >= 3 && signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF;
        var isPng = bytesRead >= 8 && signature.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var isWebp = bytesRead >= 12 && System.Text.Encoding.ASCII.GetString(signature, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(signature, 8, 4) == "WEBP";

        var valid = (isJpg && extension is ".jpg" or ".jpeg") || (isPng && extension == ".png") || (isWebp && extension == ".webp");
        if (!valid) throw new ArgumentException("Faylın formatı düzgün deyil.");

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var key = $"{folder}/{fileName}";
        var directory = Path.Combine(_environment.ContentRootPath, "uploads", folder);

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);

        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await target.WriteAsync(signature.AsMemory(0, bytesRead), ct);
        await stream.CopyToAsync(target, ct);

        return key;
    }

    public string ResolvePath(string key)
    {

        string folder;

        if (key.StartsWith("artworks/", StringComparison.Ordinal)) folder = "artworks";
        else if (key.StartsWith("slides/", StringComparison.Ordinal)) folder = "slides";
        else if (key.StartsWith("profiles/", StringComparison.Ordinal)) folder = "profiles";
        else throw new ArgumentException("Yanlış fayl ünvanı.");

        var fileName = Path.GetFileName(key);

        if (fileName != key[(folder.Length + 1)..] || string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Yanlış fayl adı.");

        return Path.Combine(_environment.ContentRootPath, "uploads", folder, fileName);

    }
}
