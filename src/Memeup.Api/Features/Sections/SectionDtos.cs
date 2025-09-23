using System.ComponentModel.DataAnnotations;

namespace Memeup.Api.Features.Sections;

public class SectionDto
{
    public Guid Id { get; set; }
    public int Status { get; set; }          // PublishStatus as int
    public string Name { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SectionCreateDto
{
    [Required, StringLength(200)]
    public string Name { get; set; } = default!;

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    public int OrderIndex { get; set; } = 0;

    /// <summary>0 = Draft, 1 = Published</summary>
    [Range(0, 1)]
    public int Status { get; set; } = 0;
}

public class SectionUpdateDto
{
    [Required, StringLength(200)]
    public string Name { get; set; } = default!;

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    public int OrderIndex { get; set; } = 0;

    /// <summary>0 = Draft, 1 = Published</summary>
    [Range(0, 1)]
    public int Status { get; set; } = 0;
}
