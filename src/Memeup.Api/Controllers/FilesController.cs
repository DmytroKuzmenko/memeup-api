using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    // Разрешённые расширения и content-types
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".webp", ".gif" };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/png", "image/jpeg", "image/webp", "image/gif" };

    // Макс. размер — 20 МБ
    private const long MaxBytes = 20 * 1024 * 1024;

    private readonly string _uploadsRoot;

    public FilesController(IWebHostEnvironment env)
    {
        _uploadsRoot = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_uploadsRoot);
    }

    /// <summary>
    /// Загрузка одного файла (multipart/form-data, поле: file).
    /// Возвращает публичный URL /uploads/...
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxBytes)] // защита от перегруза
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        if (file.Length > MaxBytes)
            return BadRequest(new { message = $"File too large. Max {MaxBytes / (1024 * 1024)} MB" });

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest(new { message = $"Invalid file extension. Allowed: {string.Join(", ", AllowedExtensions)}" });

        // content-type может быть неточным у клиента — допускаем пустой/неизвестный, но если указан и не из списка — отклоняем
        if (!string.IsNullOrEmpty(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = $"Invalid content type. Allowed: {string.Join(", ", AllowedContentTypes)}" });

        // Безопасное имя: не используем исходное, генерируем своё
        var safeName = GenerateSafeFileName(ext);
        var savePath = Path.Combine(_uploadsRoot, safeName);

        // На всякий случай убедимся, что не перезаписываем
        await using (var stream = System.IO.File.Create(savePath, 64 * 1024))
        {
            await file.CopyToAsync(stream);
        }

        // Публичный URL — через Request
        var publicPath = $"/uploads/{safeName}";
        var publicUrl = new Uri($"{Request.Scheme}://{Request.Host}{publicPath}").ToString();

        return Ok(new
        {
            url = publicUrl,
            path = publicPath,
            name = safeName,
            size = file.Length,
            contentType = file.ContentType
        });
    }

    private static string GenerateSafeFileName(string ext)
    {
        // случайное имя + расширение, чтобы не зависеть от входного имени
        // пример: 20250922_9f6c4b1c9d2b4d8f9a3c5d7e12ab34cd.png
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N");
        return $"{stamp}_{guid}{ext.ToLowerInvariant()}";
    }
}
