namespace Memeup.Api.Domain.Tasks;

public class TaskOption
{
    public Guid Id { get; set; }

    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? ImageUrl { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
}
