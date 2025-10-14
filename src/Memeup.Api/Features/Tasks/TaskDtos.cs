using System.ComponentModel.DataAnnotations;

namespace Memeup.Api.Features.Tasks;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid LevelId { get; set; }
    public int Status { get; set; }          // PublishStatus as int
    public string? InternalName { get; set; }
    public int Type { get; set; }            // TaskType as int
    public string? HeaderText { get; set; }
    public string? ImageUrl { get; set; }
    public string? ResultImagePath { get; set; }
    public string? ResultImageSource { get; set; }
    public string? TaskImageSource { get; set; }
    public string? CharsCsv { get; set; }
    public string? CorrectAnswer { get; set; }
    public TaskOptionDto[] Options { get; set; } = Array.Empty<TaskOptionDto>();
    public int OrderIndex { get; set; }
    public int? TimeLimitSec { get; set; }
    public int PointsAttempt1 { get; set; }
    public int PointsAttempt2 { get; set; }
    public int PointsAttempt3 { get; set; }
    public string? ExplanationText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class TaskOptionDto
{
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? ImageUrl { get; set; }
}

public class TaskCreateDto
{
    [Required] public Guid LevelId { get; set; }

    /// <summary>0 = Draft, 1 = Published</summary>
    [Range(0, 1)] public int Status { get; set; } = 0;

    public string? InternalName { get; set; }

    /// <summary>0=BuildWord,1=PickImage,2=PickText,3=MemeTask</summary>
    [Range(0, 3)] public int Type { get; set; } = 3;

    [StringLength(2000)]
    public string? HeaderText { get; set; }

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    [StringLength(1024)]
    public string? ResultImagePath { get; set; }

    [StringLength(1024)]
    public string? ResultImageSource { get; set; }

    [StringLength(1024)]
    public string? TaskImageSource { get; set; }

    [StringLength(1024)]
    public string? CharsCsv { get; set; }

    [StringLength(1024)]
    public string? CorrectAnswer { get; set; }
    public TaskOptionDto[] Options { get; set; } = Array.Empty<TaskOptionDto>();

    public int OrderIndex { get; set; } = 0;

    public int? TimeLimitSec { get; set; }

    public int PointsAttempt1 { get; set; } = 0;
    public int PointsAttempt2 { get; set; } = 0;
    public int PointsAttempt3 { get; set; } = 0;

    public string? ExplanationText { get; set; }
}

public class TaskUpdateDto
{
    /// <summary>0 = Draft, 1 = Published</summary>
    [Range(0, 1)] public int Status { get; set; } = 0;

    public string? InternalName { get; set; }

    /// <summary>0=BuildWord,1=PickImage,2=PickText,3=MemeTask</summary>
    [Range(0, 3)] public int Type { get; set; } = 3;

    [StringLength(2000)]
    public string? HeaderText { get; set; }

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    [StringLength(1024)]
    public string? ResultImagePath { get; set; }

    [StringLength(1024)]
    public string? ResultImageSource { get; set; }

    [StringLength(1024)]
    public string? TaskImageSource { get; set; }

    [StringLength(1024)]
    public string? CharsCsv { get; set; }

    [StringLength(1024)]
    public string? CorrectAnswer { get; set; }

    public TaskOptionDto[] Options { get; set; } = Array.Empty<TaskOptionDto>();
    
    public int OrderIndex { get; set; } = 0;

    public int? TimeLimitSec { get; set; }

    public int PointsAttempt1 { get; set; } = 0;
    public int PointsAttempt2 { get; set; } = 0;
    public int PointsAttempt3 { get; set; } = 0;

    public string? ExplanationText { get; set; }
}
