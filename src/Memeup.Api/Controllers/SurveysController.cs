using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Surveys;
using Memeup.Api.Features.Surveys;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/surveys")]
[Authorize]
public class SurveysController : ControllerBase
{
    private readonly MemeupDbContext _db;
    private readonly ISurveyDefinitionProvider _definitions;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(MemeupDbContext db, ISurveyDefinitionProvider definitions, ILogger<SurveysController> logger)
    {
        _db = db;
        _definitions = definitions;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SurveySummaryDto>>> GetSurveys(CancellationToken ct)
    {
        var includeHidden = User.IsInRole("Admin");
        var surveys = await _definitions.GetAllAsync(includeHidden, ct);
        var result = surveys.Select(s => new SurveySummaryDto(s.SurveyId, s.Title, s.Questions.Count)).ToList();
        return Ok(result);
    }

    [HttpGet("{surveyId}")]
    public async Task<ActionResult<SurveyDefinition>> GetSurvey(string surveyId, CancellationToken ct)
    {
        var includeHidden = User.IsInRole("Admin");
        var survey = await _definitions.GetByIdAsync(surveyId, includeHidden, ct);
        if (survey == null)
        {
            return NotFound();
        }

        // Если пользователь уже проходил опрос — возвращаем конфликт, чтобы фронт не показывал повторно
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var alreadySubmitted = await _db.SurveyResponses.AnyAsync(r => r.SurveyId == surveyId && r.UserId == userId, ct);
            if (alreadySubmitted)
            {
                return Conflict(new { message = "Survey already submitted" });
            }
        }

        return Ok(survey);
    }

    [HttpPost("{surveyId}/responses")]
    public async Task<ActionResult> SubmitResponse(string surveyId, [FromBody] SurveyResponseRequest request, CancellationToken ct)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Answers == null || request.Answers.Count == 0)
        {
            return BadRequest(new { message = "At least one answer is required" });
        }

        var includeHidden = User.IsInRole("Admin");
        var survey = await _definitions.GetByIdAsync(surveyId, includeHidden, ct);
        if (survey == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var alreadySubmitted = await _db.SurveyResponses.AnyAsync(r => r.SurveyId == surveyId && r.UserId == userId, ct);
        if (alreadySubmitted)
        {
            return Conflict(new { message = "Survey already submitted" });
        }

        var questionLookup = survey.Questions.ToDictionary(q => q.Id, StringComparer.OrdinalIgnoreCase);
        var normalizedAnswers = new List<SurveyAnswer>();

        foreach (var answer in request.Answers.DistinctBy(a => a.QuestionId))
        {
            if (!questionLookup.TryGetValue(answer.QuestionId, out var question))
            {
                return BadRequest(new { message = $"Unknown question id: {answer.QuestionId}" });
            }

            if (!answer.TryGetValue(out var rawValue))
            {
                return BadRequest(new { message = $"Question '{answer.QuestionId}' requires a value" });
            }

            var parsedValue = ParseAnswer(question, rawValue, out var errorMessage);
            if (errorMessage != null)
            {
                return BadRequest(new { message = errorMessage });
            }

            normalizedAnswers.Add(new SurveyAnswer
            {
                QuestionId = question.Id,
                Value = parsedValue
            });
        }

        var response = new SurveyResponse
        {
            SurveyId = surveyId,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            Answers = normalizedAnswers
        };

        _db.SurveyResponses.Add(response);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetSurvey), new { surveyId }, new { id = response.Id });
    }

    [HttpGet("admin/list")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<SurveyDefinition>>> GetAdminList(CancellationToken ct)
    {
        var surveys = await _definitions.GetAllAsync(includeHidden: true, ct);
        return Ok(surveys);
    }

    [HttpPost("admin/save")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SurveyDefinition>> SaveDefinition([FromBody] SurveyDefinition definition, CancellationToken ct)
    {
        try
        {
            var saved = await _definitions.SaveAsync(definition, ct);
            return Ok(saved);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save survey definition {SurveyId}", definition?.SurveyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to save survey" });
        }
    }

    private static string? ParseAnswer(SurveyQuestionDefinition question, JsonElement value, out string? error)
    {
        error = null;
            switch (question.Type)
            {
                case SurveyQuestionType.SingleChoice:
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        error = $"Question '{question.Id}' expects a string option";
                        return null;
                    }
                    var selected = value.GetString() ?? string.Empty;
                    if (question.Options == null || !question.Options.Contains(selected))
                    {
                        error = $"Question '{question.Id}' has invalid option";
                        return null;
                    }
                    return selected;
                case SurveyQuestionType.Scale:
                int scaleValue;
                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                {
                    scaleValue = parsed;
                }
                else if (!value.TryGetInt32(out scaleValue))
                {
                    error = $"Question '{question.Id}' expects a numeric value";
                    return null;
                }
                var min = question.ScaleMin ?? int.MinValue;
                var max = question.ScaleMax ?? int.MaxValue;
                if (scaleValue < min || scaleValue > max)
                {
                    error = $"Question '{question.Id}' value must be between {min} and {max}";
                    return null;
                }
                return scaleValue.ToString();
            case SurveyQuestionType.ShortText:
                if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null or JsonValueKind.Undefined))
                {
                    error = $"Question '{question.Id}' expects a text value";
                    return null;
                }
                return value.GetString() ?? string.Empty;
            default:
                error = $"Unsupported question type for '{question.Id}'";
                return null;
        }
    }
}
