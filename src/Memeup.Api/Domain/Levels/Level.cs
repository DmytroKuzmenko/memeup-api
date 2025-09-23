using Memeup.Api.Domain.Abstractions;
using Memeup.Api.Domain.Sections;
using Memeup.Api.Domain.Tasks;

namespace Memeup.Api.Domain.Levels;

public class Level : BaseEntity
{
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public string? HeaderText { get; set; }
    public string? AnimationImageUrl { get; set; }
    public int OrderIndex { get; set; } = 0;
    public int? TimeLimitSec { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
