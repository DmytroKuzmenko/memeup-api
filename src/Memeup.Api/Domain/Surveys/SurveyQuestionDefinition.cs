using System.Text.Json.Serialization;
using Memeup.Api.Features.Surveys;

namespace Memeup.Api.Domain.Surveys;

public class SurveyQuestionDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [JsonConverter(typeof(SurveyQuestionTypeConverter))]
    public SurveyQuestionType Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    [JsonPropertyName("scaleMin")]
    public int? ScaleMin { get; set; }

    [JsonPropertyName("scaleMax")]
    public int? ScaleMax { get; set; }
}
