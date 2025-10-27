using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Memeup.Api.Controllers;

public sealed class FileUploadRequest
{
    /// <summary>Файл изображения или видео (png, jpg, jpeg, webp, gif, mp4)</summary>
    public IFormFile File { get; set; } = default!;
}

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".mp4" };

    private const long MaxFileSize = 20 * 1024 * 1024; // 20 MB

     private readonly IWebHostEnvironment _environment;

    public FilesController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] // как и просили — write закрыт
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    [RequestSizeLimit(MaxFileSize)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] FileUploadRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { error = "file is required" });

        if (request.File.Length > MaxFileSize)
            return BadRequest(new { error = "file too large (max 20MB)" });

        var ext = Path.GetExtension(request.File.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !Allowed.Contains(ext))
            return BadRequest(new { error = "unsupported file type" });

        // генерим имя и сохраняем
         var uploadsRoot = Path.Combine(_environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var fs = System.IO.File.Create(fullPath))
        {
            await request.File.CopyToAsync(fs, ct);
        }

        // публичный URL (у тебя раздача /uploads/* уже настроена в Program.cs)
        var publicUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
        return Ok(new { url = publicUrl });
    }
}
