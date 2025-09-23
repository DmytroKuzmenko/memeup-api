using System.ComponentModel.DataAnnotations;

namespace Memeup.Api.Features.Levels;

public class LevelDto
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public int Status { get; set; }          // PublishStatus as int
    public string Name { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public string? HeaderText { get; set; }
    public string? AnimationImageUrl { get; set; }
    public int OrderIndex { get; set; }
    public int? TimeLimitSec { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class LevelCreateDto
{
    [Required] public Guid SectionId { get; set; }

    /// <summary>0 = Draft, 1 = Published</summary>
    [Range(0, 1)] public int Status { get; set; } = 0;

    [Required, StringLength(200)]
    public string Name { get; set; } = default!;

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    [StringLength(2000)]
    public string? HeaderText { get; set; }

    [StringLength(1024)]
    public string? AnimationImageUrl { get; set; }

    public int OrderIndex { get; set; } = 0;

    public int? TimeLimitSec { get; set; }
}

public class LevelUpdateDto
{
    /// <summary>0 = Draft, 1 = Published</summary>
    [Range(0, 1)] public int Status { get; set; } = 0;

    [Required, StringLength(200)]
    public string Name { get; set; } = default!;

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    [StringLength(2000)]
    public string? HeaderText { get; set; }

    [StringLength(1024)]
    public string? AnimationImageUrl { get; set; }

    public int OrderIndex { get; set; } = 0;

    public int? TimeLimitSec { get; set; }
}
