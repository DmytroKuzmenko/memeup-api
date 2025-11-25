namespace Memeup.Api.Domain.Surveys;

public class SurveyResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SurveyId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
