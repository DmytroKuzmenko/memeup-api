using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Memeup.Api.Features.Surveys;

public class SurveyResponseRequest
{
    [Required]
    public List<SurveyAnswerRequest> Answers { get; set; } = new();
}

public class SurveyAnswerRequest
{
    [Required]
    public string QuestionId { get; set; } = string.Empty;

    // Клиент может прислать либо "value", либо legacy "answer"
    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }

    [JsonPropertyName("answer")]
    public JsonElement? LegacyValue { get; set; }

    public bool TryGetValue(out JsonElement value)
    {
        if (Value.ValueKind != JsonValueKind.Undefined)
        {
            value = Value;
            return true;
        }

        if (LegacyValue is JsonElement legacy && legacy.ValueKind != JsonValueKind.Undefined)
        {
            value = legacy;
            return true;
        }

        value = default;
        return false;
    }
}
