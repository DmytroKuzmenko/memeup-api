using System.Collections.Generic;
using Memeup.Api.Domain.Abstractions;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Levels;

namespace Memeup.Api.Domain.Tasks;

public class TaskItem : BaseEntity
{
    public Guid LevelId { get; set; }
    public Level Level { get; set; } = default!;

    public string? InternalName { get; set; }
    public TaskType Type { get; set; } = TaskType.MemeTask;

    public string? HeaderText { get; set; }
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; } = 0;
    public int? TimeLimitSec { get; set; }

    public int PointsAttempt1 { get; set; } = 0;
    public int PointsAttempt2 { get; set; } = 0;
    public int PointsAttempt3 { get; set; } = 0;

    public string? ExplanationText { get; set; }

    public List<TaskOption> Options { get; set; } = new();
}
