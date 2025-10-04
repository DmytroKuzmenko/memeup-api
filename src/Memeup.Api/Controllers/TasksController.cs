using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Tasks;
using Memeup.Api.Features.Tasks;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly MemeupDbContext _db;
    private readonly IMapper _mapper;

    public TasksController(MemeupDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // GET /api/levels/{levelId}/tasks  (по контракту)
    [HttpGet("/api/levels/{levelId:guid}/tasks")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetByLevel(Guid levelId)
    {
        var exists = await _db.Levels.AnyAsync(l => l.Id == levelId);
        if (!exists) return NotFound();

        var items = await _db.Tasks
            .Where(t => t.LevelId == levelId)
            .OrderBy(t => t.OrderIndex).ThenBy(t => t.InternalName)
            .ProjectTo<TaskDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/tasks/{id}
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TaskDto>> GetById(Guid id)
    {
        var entity = await _db.Tasks
            .Include(t => t.Options)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return NotFound();
        return Ok(_mapper.Map<TaskDto>(entity));
    }

    // POST /api/tasks  (Admin)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TaskDto>> Create(TaskCreateDto dto)
    {
        // FK check
        var levelExists = await _db.Levels.AnyAsync(l => l.Id == dto.LevelId);
        if (!levelExists) return BadRequest(new { message = "Level not found" });

        var entity = _mapper.Map<TaskItem>(dto);
        _db.Tasks.Add(entity);
        await _db.SaveChangesAsync();

        var result = _mapper.Map<TaskDto>(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    // PUT /api/tasks/{id}  (Admin)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TaskDto>> Update(Guid id, TaskUpdateDto dto)
    {
        var entity = await _db.Tasks
            .Include(t => t.Options)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return NotFound();

        entity.Status = (PublishStatus)dto.Status;
        entity.InternalName = dto.InternalName;
        entity.Type = (TaskType)dto.Type;
        entity.HeaderText = dto.HeaderText;
        entity.ImageUrl = dto.ImageUrl;

        var optionDtos = dto.Options ?? Array.Empty<TaskOptionDto>();

        var existingById = entity.Options.ToDictionary(o => o.Id);
        var incomingIds = optionDtos
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        var toRemove = entity.Options
            .Where(o => !incomingIds.Contains(o.Id))
            .ToList();

        foreach (var option in toRemove)
        {
            entity.Options.Remove(option);
        }

        foreach (var optionDto in optionDtos)
        {
            if (optionDto.Id.HasValue && existingById.TryGetValue(optionDto.Id.Value, out var existing))
            {
                existing.Label = optionDto.Label;
                existing.IsCorrect = optionDto.IsCorrect;
                existing.ImageUrl = optionDto.ImageUrl;
                continue;
            }

            entity.Options.Add(new TaskOption
            {
                Id = optionDto.Id ?? Guid.NewGuid(),
                Label = optionDto.Label,
                IsCorrect = optionDto.IsCorrect,
                ImageUrl = optionDto.ImageUrl,
            });
        }

        entity.OrderIndex = dto.OrderIndex;
        entity.TimeLimitSec = dto.TimeLimitSec;
        entity.PointsAttempt1 = dto.PointsAttempt1;
        entity.PointsAttempt2 = dto.PointsAttempt2;
        entity.PointsAttempt3 = dto.PointsAttempt3;
        entity.ExplanationText = dto.ExplanationText;

        await _db.SaveChangesAsync();

        return Ok(_mapper.Map<TaskDto>(entity));
    }

    // DELETE /api/tasks/{id} (Admin)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Tasks.FindAsync(id);
        if (entity == null) return NotFound();

        _db.Tasks.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
