using System.Text.Json;
using System.Text.Json.Serialization;
using Memeup.Api.Domain.Surveys;

namespace Memeup.Api.Features.Surveys;

public class FileSystemSurveyDefinitionProvider : ISurveyDefinitionProvider
{
    private readonly string _surveysRoot;
    private readonly ILogger<FileSystemSurveyDefinitionProvider> _logger;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly Dictionary<string, SurveyDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _sync = new(1, 1);

    public FileSystemSurveyDefinitionProvider(IWebHostEnvironment env, ILogger<FileSystemSurveyDefinitionProvider> logger)
    {
        _surveysRoot = Path.Combine(env.ContentRootPath, "Surveys");
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new SurveyQuestionTypeConverter() }
        };
        Directory.CreateDirectory(_surveysRoot);
    }

    public async Task<IEnumerable<SurveyDefinition>> GetAllAsync(bool includeHidden = false, CancellationToken ct = default)
    {
        await _sync.WaitAsync(ct);
        try
        {
            return _definitions.Values
                .Where(s => includeHidden || !s.IsHidden)
                .Select(Clone)
                .ToList();
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<SurveyDefinition?> GetByIdAsync(string surveyId, bool includeHidden = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(surveyId)) return null;

        await _sync.WaitAsync(ct);
        try
        {
            if (!_definitions.TryGetValue(surveyId, out var definition)) return null;
            if (!includeHidden && definition.IsHidden) return null;
            return Clone(definition);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await _sync.WaitAsync(ct);
        try
        {
            _definitions.Clear();
            if (!Directory.Exists(_surveysRoot))
            {
                Directory.CreateDirectory(_surveysRoot);
                return;
            }

            var files = Directory.GetFiles(_surveysRoot, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    await using var stream = File.OpenRead(file);
                    var definition = await JsonSerializer.DeserializeAsync<SurveyDefinition>(stream, _serializerOptions, ct);
                    if (definition == null)
                    {
                        _logger.LogWarning("Survey file {File} is empty or invalid", file);
                        continue;
                    }

                    ValidateDefinition(definition);
                    _definitions[definition.SurveyId] = definition;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load survey file {File}", file);
                }
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<SurveyDefinition> SaveAsync(SurveyDefinition definition, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ValidateDefinition(definition);

        var safeFileName = Path.GetFileName(definition.SurveyId);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new InvalidOperationException("SurveyId contains invalid characters");
        }
        var targetPath = Path.Combine(_surveysRoot, $"{safeFileName}.json");

        await _sync.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(definition, _serializerOptions);
            await File.WriteAllTextAsync(targetPath, json, ct);
            _definitions[definition.SurveyId] = Clone(definition);
            return definition;
        }
        finally
        {
            _sync.Release();
        }
    }

    private void ValidateDefinition(SurveyDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.SurveyId))
        {
            throw new InvalidOperationException("SurveyId is required");
        }

        if (string.IsNullOrWhiteSpace(definition.Title))
        {
            throw new InvalidOperationException("Title is required");
        }

        if (definition.Questions == null || definition.Questions.Count == 0)
        {
            throw new InvalidOperationException("At least one question is required");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var question in definition.Questions)
        {
            if (question == null)
            {
                throw new InvalidOperationException("Question entry cannot be null");
            }

            if (string.IsNullOrWhiteSpace(question.Id))
            {
                throw new InvalidOperationException("Question id is required");
            }

            if (!ids.Add(question.Id))
            {
                throw new InvalidOperationException($"Duplicate question id: {question.Id}");
            }

            if (string.IsNullOrWhiteSpace(question.Text))
            {
                throw new InvalidOperationException($"Question '{question.Id}' text is required");
            }

            switch (question.Type)
            {
                case SurveyQuestionType.SingleChoice:
                    if (question.Options == null || question.Options.Count == 0)
                    {
                        throw new InvalidOperationException($"Question '{question.Id}' must contain options");
                    }
                    break;
                case SurveyQuestionType.Scale:
                    if (question.ScaleMin is null || question.ScaleMax is null)
                    {
                        throw new InvalidOperationException($"Question '{question.Id}' must define scaleMin and scaleMax");
                    }

                    if (question.ScaleMin > question.ScaleMax)
                    {
                        throw new InvalidOperationException($"Question '{question.Id}' scaleMin must be less than or equal to scaleMax");
                    }
                    break;
                case SurveyQuestionType.ShortText:
                    break;
                default:
                    throw new InvalidOperationException($"Question '{question.Id}' has unsupported type");
            }
        }
    }

    private SurveyDefinition Clone(SurveyDefinition definition)
    {
        var json = JsonSerializer.Serialize(definition, _serializerOptions);
        return JsonSerializer.Deserialize<SurveyDefinition>(json, _serializerOptions) ?? definition;
    }
}
