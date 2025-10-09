using System;

namespace Memeup.Api.Domain.Game;

public class ActiveTaskAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Token { get; set; }
    public Guid UserId { get; set; }
    public Guid LevelId { get; set; }
    public Guid TaskId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTimeOffset AttemptStartAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsFinalized { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
