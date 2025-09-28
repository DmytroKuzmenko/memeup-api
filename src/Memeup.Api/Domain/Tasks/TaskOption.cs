using System;

namespace Memeup.Api.Domain.Tasks;

public class TaskOption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = default!;

    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; } = 0;
}
