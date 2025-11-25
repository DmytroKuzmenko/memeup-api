namespace Memeup.Api.Domain.Surveys;

public class SurveyAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SurveyResponseId { get; set; }
    public string QuestionId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public SurveyResponse? SurveyResponse { get; set; }
}
