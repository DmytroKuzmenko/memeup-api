using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Tasks;
using Memeup.Api.Features.Tasks;

namespace Memeup.Api.Controllers;


using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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



  [HttpPut("{id:guid}")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<TaskDto>> Update(Guid id, TaskUpdateDto dto)
{
    // Нормализация опций (Guid.Empty => null)
    var incoming = (dto.Options ?? Array.Empty<TaskOptionDto>())
        .Select(o => new
        {
            Id = (o.Id.HasValue && o.Id.Value != Guid.Empty) ? o.Id : null,
            o.Label,
            o.IsCorrect,
            o.ImageUrl
        })
        .ToList();

    var strategy = _db.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync(async () =>
    {
        // ===== Фаза 1: сохраняем только скалярные поля Task =====
        var task = await _db.Tasks.AsTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return (ActionResult<TaskDto>)NotFound();

        task.Status = (PublishStatus)dto.Status;
        task.InternalName = dto.InternalName;
        task.Type = (TaskType)dto.Type;
        task.HeaderText = dto.HeaderText;
        task.ImageUrl = dto.ImageUrl;
        task.OrderIndex = dto.OrderIndex;
        task.TimeLimitSec = dto.TimeLimitSec;
        task.PointsAttempt1 = dto.PointsAttempt1;
        task.PointsAttempt2 = dto.PointsAttempt2;
        task.PointsAttempt3 = dto.PointsAttempt3;
        task.ExplanationText = dto.ExplanationText;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return (ActionResult<TaskDto>)Conflict("Конфликт обновления Task: данные были изменены или удалены. Обновите и повторите.");
        }

        // ===== Фаза 2: работаем ТОЛЬКО с Options, не трогая владельца =====
        // Загружаем владельца с коллекцией
        var entity = await _db.Tasks
            .Include(t => t.Options)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return (ActionResult<TaskDto>)NotFound();

        entity.Options ??= new List<TaskOption>();

        // КЛЮЧЕВОЕ: запрещаем EF апдейтить владельца
        _db.Entry(entity).State = EntityState.Unchanged;

        // Индексы существующих по Id
        var existingById = entity.Options.ToDictionary(x => x.Id, x => x);
        var incomingIds = incoming.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();

        // Удаление отсутствующих (через навигацию; владелец Unchanged)
        foreach (var toDel in entity.Options.Where(x => !incomingIds.Contains(x.Id)).ToList())
        {
            entity.Options.Remove(toDel);
            // Для owned EF сам пометит удаление зависимой строки
        }

        // Добавление/обновление с явными состояниями зависимых
        foreach (var inc in incoming)
        {
            if (inc.Id.HasValue && existingById.TryGetValue(inc.Id.Value, out var exist))
            {
                exist.Label = inc.Label;
                exist.IsCorrect = inc.IsCorrect;
                exist.ImageUrl = inc.ImageUrl;

                // ЯВНО: изменяемая зависимая
                _db.Entry(exist).State = EntityState.Modified;
            }
            else
            {
                var created = new TaskOption
                {
                    Id = Guid.NewGuid(),
                    Label = inc.Label,
                    IsCorrect = inc.IsCorrect,
                    ImageUrl = inc.ImageUrl
                };
                entity.Options.Add(created);

                // ЯВНО: новая зависимая
                _db.Entry(created).State = EntityState.Added;
            }
        }

        try
        {
            await _db.SaveChangesAsync(); // здесь должны уйти только INSERT/DELETE/UPDATE по TaskOptions
        }
        catch (DbUpdateConcurrencyException)
        {
            return (ActionResult<TaskDto>)Conflict("Конфликт при обновлении опций: данные были изменены. Обновите и повторите.");
        }

        return (ActionResult<TaskDto>)Ok(_mapper.Map<TaskDto>(entity));
    });
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
