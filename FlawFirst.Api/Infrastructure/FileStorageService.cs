using FlawFirst.Api.Common;

namespace FlawFirst.Api.Infrastructure;

public interface IFileStorageService
{
    Task<IReadOnlyList<string>> SaveImagesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken);
}

public class FileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private const long MaxBytes = 5 * 1024 * 1024;
    private const int MaxFiles = 6;
    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<IReadOnlyList<string>> SaveImagesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        var list = files.Where(file => file.Length > 0).ToList();
        if (list.Count == 0)
            throw new AppException(StatusCodes.Status400BadRequest, "Vui lòng chọn ít nhất một ảnh.");

        if (list.Count > MaxFiles)
            throw new AppException(StatusCodes.Status400BadRequest, $"Chỉ được tải tối đa {MaxFiles} ảnh.");

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var month = DateTime.UtcNow.ToString("yyyyMM");
        var folder = Path.Combine(webRoot, "uploads", month);
        Directory.CreateDirectory(folder);

        var urls = new List<string>(list.Count);
        foreach (var file in list)
        {
            if (file.Length > MaxBytes)
                throw new AppException(StatusCodes.Status400BadRequest, $"Ảnh '{file.FileName}' vượt quá 5MB.");

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
                throw new AppException(StatusCodes.Status400BadRequest, "Chỉ chấp nhận ảnh jpg, jpeg, png, gif, webp.");

            var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var physicalPath = Path.Combine(folder, storedName);
            await using var stream = new FileStream(physicalPath, FileMode.CreateNew);
            await file.CopyToAsync(stream, cancellationToken);
            urls.Add($"/uploads/{month}/{storedName}");
        }

        return urls;
    }
}
