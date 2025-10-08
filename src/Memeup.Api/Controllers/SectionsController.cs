using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Sections;
using Memeup.Api.Features.Sections;
using Memeup.Api.Domain.Enums;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SectionsController : ControllerBase
{
    private readonly MemeupDbContext _db;
    private readonly IMapper _mapper;

    public SectionsController(MemeupDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // GET /api/sections  (MVP: без пагинации/фильтров)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SectionDto>>> GetAll()
    {
        var items = await _db.Sections
            .OrderBy(s => s.OrderIndex)
            .ThenBy(s => s.Name)
            .ProjectTo<SectionDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/sections/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SectionDto>> GetById(Guid id)
    {
        var entity = await _db.Sections.FindAsync(id);
        if (entity == null) return NotFound();

        return Ok(_mapper.Map<SectionDto>(entity));
    }

    // POST /api/sections   (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SectionDto>> Create([FromBody] SectionCreateDto dto)
    {
        var entity = _mapper.Map<Section>(dto);
        _db.Sections.Add(entity);
        await _db.SaveChangesAsync();

        var result = _mapper.Map<SectionDto>(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    // PUT /api/sections/{id}  (Admin only)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SectionDto>> Update(Guid id, [FromBody] SectionUpdateDto dto)
    {
        var entity = await _db.Sections.FindAsync(id);
        if (entity == null) return NotFound();

        // map onto existing entity
        entity.Name = dto.Name;
        entity.ImageUrl = dto.ImageUrl;
        entity.OrderIndex = dto.OrderIndex;
        entity.Status = (PublishStatus)dto.Status;

        await _db.SaveChangesAsync();

        return Ok(_mapper.Map<SectionDto>(entity));
    }

    // DELETE /api/sections/{id}  (Admin only)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Sections.FindAsync(id);
        if (entity == null) return NotFound();

        _db.Sections.Remove(entity);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
