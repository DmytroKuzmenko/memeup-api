using System;

namespace Memeup.Api.Domain.Game;

public static class LevelProgressStatuses
{
    public const string NotStarted = "NotStarted";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Locked = "Locked";
}

public class UserLevelProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid LevelId { get; set; }
    public string Status { get; set; } = LevelProgressStatuses.NotStarted;
    public Guid? LastTaskId { get; set; }
    public int LastRunScore { get; set; }
    public int BestScore { get; set; }
    public int MaxScore { get; set; }
    public int RunsCount { get; set; }
    public DateTimeOffset? LastCompletedAt { get; set; }
    public DateTimeOffset? ReplayAvailableAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
