using System;

namespace Memeup.Api.Domain.Game;

public class UserTaskProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid LevelId { get; set; }
    public Guid TaskId { get; set; }
    public int AttemptsUsed { get; set; }
    public int PointsEarned { get; set; }
    public bool IsCompleted { get; set; }
    public int TimeSpentSec { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
