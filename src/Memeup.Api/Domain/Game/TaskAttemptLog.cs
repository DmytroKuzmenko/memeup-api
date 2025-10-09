using System;

namespace Memeup.Api.Domain.Game;

public class TaskAttemptLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid LevelId { get; set; }
    public Guid TaskId { get; set; }
    public int AttemptNumber { get; set; }
    public bool IsCorrect { get; set; }
    public bool IsTimeout { get; set; }
    public int TimeSpentSec { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public int PointsAwarded { get; set; }
    public bool ShownExplanation { get; set; }
    public string? ClientAgent { get; set; }
    public string? ClientTz { get; set; }
    public string? IpHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
