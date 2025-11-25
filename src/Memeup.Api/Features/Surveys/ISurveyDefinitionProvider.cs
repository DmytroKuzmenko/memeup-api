using Memeup.Api.Domain.Surveys;

namespace Memeup.Api.Features.Surveys;

public interface ISurveyDefinitionProvider
{
    Task<IEnumerable<SurveyDefinition>> GetAllAsync(bool includeHidden = false, CancellationToken ct = default);
    Task<SurveyDefinition?> GetByIdAsync(string surveyId, bool includeHidden = false, CancellationToken ct = default);
    Task ReloadAsync(CancellationToken ct = default);
    Task<SurveyDefinition> SaveAsync(SurveyDefinition definition, CancellationToken ct = default);
}
