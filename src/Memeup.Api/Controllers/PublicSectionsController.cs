using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Features.Sections;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/public/sections")]
[Authorize]
public class PublicSectionsController : ControllerBase
{
    private readonly MemeupDbContext _db;

    public PublicSectionsController(MemeupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PublicSectionDto>>> GetPublishedSections()
    {
        var sections = await _db.Sections
            .Where(s => s.Status == PublishStatus.Published)
            .OrderBy(s => s.OrderIndex)
            .Select(s => new PublicSectionDto
            {
                Id = s.Id,
                Name = s.Name,
                ImageUrl = s.ImageUrl,
                CompletedLevelsCount = 0,
                TotalLevelsCount = s.Levels.Count()
            })
            .ToListAsync();

        return Ok(sections);
    }
}
