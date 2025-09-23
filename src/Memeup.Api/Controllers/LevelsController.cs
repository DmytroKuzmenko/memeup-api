using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Levels;
using Memeup.Api.Features.Levels;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LevelsController : ControllerBase
{
    private readonly MemeupDbContext _db;
    private readonly IMapper _mapper;

    public LevelsController(MemeupDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // GET /api/sections/{sectionId}/levels  (по контракту)
    [HttpGet("/api/sections/{sectionId:guid}/levels")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LevelDto>>> GetBySection(Guid sectionId)
    {
        var exists = await _db.Sections.AnyAsync(s => s.Id == sectionId);
        if (!exists) return NotFound();

        var items = await _db.Levels
            .Where(l => l.SectionId == sectionId)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Name)
            .ProjectTo<LevelDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/levels/{id}
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<LevelDto>> GetById(Guid id)
    {
        var entity = await _db.Levels.FindAsync(id);
        if (entity == null) return NotFound();
        return Ok(_mapper.Map<LevelDto>(entity));
    }

    // POST /api/levels   (Admin)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LevelDto>> Create(LevelCreateDto dto)
    {
        // FK check
        var sectionExists = await _db.Sections.AnyAsync(s => s.Id == dto.SectionId);
        if (!sectionExists) return BadRequest(new { message = "Section not found" });

        var entity = _mapper.Map<Level>(dto);
        _db.Levels.Add(entity);
        await _db.SaveChangesAsync();

        var result = _mapper.Map<LevelDto>(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    // PUT /api/levels/{id}   (Admin)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LevelDto>> Update(Guid id, LevelUpdateDto dto)
    {
        var entity = await _db.Levels.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Status = (PublishStatus)dto.Status;
        entity.Name = dto.Name;
        entity.ImageUrl = dto.ImageUrl;
        entity.HeaderText = dto.HeaderText;
        entity.AnimationImageUrl = dto.AnimationImageUrl;
        entity.OrderIndex = dto.OrderIndex;
        entity.TimeLimitSec = dto.TimeLimitSec;

        await _db.SaveChangesAsync();
        return Ok(_mapper.Map<LevelDto>(entity));
    }

    // DELETE /api/levels/{id}  (Admin)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Levels.FindAsync(id);
        if (entity == null) return NotFound();

        _db.Levels.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
