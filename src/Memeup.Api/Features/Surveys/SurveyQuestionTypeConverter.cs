using System.Text.Json;
using System.Text.Json.Serialization;
using Memeup.Api.Domain.Surveys;

namespace Memeup.Api.Features.Surveys;

public class SurveyQuestionTypeConverter : JsonConverter<SurveyQuestionType>
{
    public override SurveyQuestionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.ToLowerInvariant() switch
        {
            "single-choice" => SurveyQuestionType.SingleChoice,
            "scale" => SurveyQuestionType.Scale,
            "short-text" => SurveyQuestionType.ShortText,
            _ => throw new JsonException($"Unsupported survey question type '{value}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, SurveyQuestionType value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            SurveyQuestionType.SingleChoice => "single-choice",
            SurveyQuestionType.Scale => "scale",
            SurveyQuestionType.ShortText => "short-text",
            _ => throw new JsonException($"Unsupported survey question type '{value}'")
        };
        writer.WriteStringValue(str);
    }
}
