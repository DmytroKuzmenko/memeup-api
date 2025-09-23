using Memeup.Api.Domain.Abstractions;
using Memeup.Api.Domain.Levels;

namespace Memeup.Api.Domain.Sections;

public class Section : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; } = 0;

    public ICollection<Level> Levels { get; set; } = new List<Level>();
}
