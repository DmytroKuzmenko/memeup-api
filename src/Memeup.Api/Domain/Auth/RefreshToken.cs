using System.ComponentModel.DataAnnotations;

namespace Memeup.Api.Domain.Auth;

public class RefreshToken
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    [Required]
    [MaxLength(256)]
    public string TokenHash { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public int UsageCount { get; set; }

    public bool CanBeUsed(int maxReuseCount) =>
        RevokedAt is null &&
        ExpiresAt > DateTimeOffset.UtcNow &&
        UsageCount < maxReuseCount;
}
