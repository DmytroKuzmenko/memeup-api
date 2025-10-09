using System;

namespace Memeup.Api.Domain.Game;

public class LeaderboardEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Period { get; set; } = "AllTime";
    public int Score { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
