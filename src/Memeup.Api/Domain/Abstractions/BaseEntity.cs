namespace Memeup.Api.Domain.Abstractions;

using Memeup.Api.Domain.Enums;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public PublishStatus Status { get; set; } = PublishStatus.Draft;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

}
