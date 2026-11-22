using System.Text.RegularExpressions;

namespace Majstorio.WebAPI.Services;

// Saves profile photos as files in one folder and reads them back.
// Only JPEG and PNG are accepted, recognised by the first bytes of the file rather than by its name,
// and each file gets a new random name, so a client cannot choose where a file is written.
public sealed partial class ProfileImageStore
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly byte[] JpegStart = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngStart = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly string _folder;

    // The folder comes from ProfileImages__Path; by default "uploads/profile-images" next to the API.
    public ProfileImageStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var path = configuration["ProfileImages:Path"];
        _folder = Path.GetFullPath(string.IsNullOrWhiteSpace(path)
            ? Path.Combine(environment.ContentRootPath, "uploads", "profile-images")
            : path);
    }

    public ProfileImageStore(string folder) => _folder = Path.GetFullPath(folder);

    // Returns the new file name, or an error message for the user when the file is not accepted.
    public async Task<(string? FileName, string? Error)> SaveAsync(Stream content, long length)
    {
        if (length <= 0)
            return (null, "The file is empty.");
        if (length > MaxBytes)
            return (null, "The photo can be at most 2 MB.");

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        if (bytes.Length > MaxBytes)
            return (null, "The photo can be at most 2 MB.");

        string extension;
        if (StartsWith(bytes, JpegStart))
            extension = ".jpg";
        else if (StartsWith(bytes, PngStart))
            extension = ".png";
        else
            return (null, "Only JPEG and PNG photos are allowed.");

        Directory.CreateDirectory(_folder);
        var fileName = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(_folder, fileName), bytes);
        return (fileName, null);
    }

    // The file, or null when the name is not one this store created or the file is gone.
    public (string Path, string ContentType)? Find(string fileName)
    {
        if (!FileNamePattern().IsMatch(fileName))
            return null;
        var path = Path.Combine(_folder, fileName);
        if (!File.Exists(path))
            return null;
        return (path, fileName.EndsWith(".png", StringComparison.Ordinal) ? "image/png" : "image/jpeg");
    }

    public void Delete(string? fileName)
    {
        if (fileName == null || !FileNamePattern().IsMatch(fileName))
            return;
        var path = Path.Combine(_folder, fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static bool StartsWith(byte[] bytes, byte[] start)
        => bytes.Length >= start.Length && bytes.AsSpan(0, start.Length).SequenceEqual(start);

    // A 32-character hex name with .jpg or .png: exactly what SaveAsync creates, so no path can be smuggled in.
    [GeneratedRegex("^[0-9a-f]{32}\\.(jpg|png)$")]
    private static partial Regex FileNamePattern();
}
