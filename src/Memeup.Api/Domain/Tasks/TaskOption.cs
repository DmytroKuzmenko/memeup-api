namespace Memeup.Api.Domain.Tasks;

public class TaskOption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? ImageUrl { get; set; }
}
