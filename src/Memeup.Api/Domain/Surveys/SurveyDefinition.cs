using System.Text.Json.Serialization;

namespace Memeup.Api.Domain.Surveys;

public class SurveyDefinition
{
    [JsonPropertyName("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("questions")]
    public List<SurveyQuestionDefinition> Questions { get; set; } = new();
}
