using System;

namespace Memeup.Api.Domain.Game;

public class UserSectionProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid SectionId { get; set; }
    public int LevelsCompleted { get; set; }
    public int TotalLevels { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
