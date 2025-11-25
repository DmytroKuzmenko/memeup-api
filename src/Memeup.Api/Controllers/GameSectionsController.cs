using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Features.Game;
using Memeup.Api.Domain.Game;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/game/sections")]
[Authorize]
public class GameSectionsController : ControllerBase
{
    private readonly MemeupDbContext _db;

    public GameSectionsController(MemeupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GameSectionDto>>> GetSections(CancellationToken ct)
    {
        ApplyNoCacheHeaders();
        var userId = GetCurrentUserId();

        var sections = await _db.Sections
            .Where(s => s.Status == PublishStatus.Published)
            .OrderBy(s => s.OrderIndex).ThenBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.ImageUrl, s.OrderIndex })
            .ToListAsync(ct);

        if (sections.Count == 0)
        {
            return Ok(Array.Empty<GameSectionDto>());
        }

        var sectionIds = sections.Select(s => s.Id).ToList();

        var levels = await _db.Levels
            .Where(l => sectionIds.Contains(l.SectionId) && l.Status == PublishStatus.Published)
            .Select(l => new { l.Id, l.SectionId, l.OrderIndex })
            .ToListAsync(ct);

        var levelIds = levels.Select(l => l.Id).ToList();

        var levelProgress = await _db.UserLevelProgress
            .Where(p => p.UserId == userId && levelIds.Contains(p.LevelId))
            .Select(p => new { p.LevelId, p.Status, p.BestScore, p.MaxScore })
            .ToListAsync(ct);

        var levelMaxScores = await _db.Tasks
            .Where(t => levelIds.Contains(t.LevelId) && t.Status == PublishStatus.Published)
            .GroupBy(t => t.LevelId)
            .Select(g => new { LevelId = g.Key, MaxScore = g.Sum(t => t.PointsAttempt1) })
            .ToListAsync(ct);

        var progressLookup = levelProgress.ToDictionary(x => x.LevelId, x => x);
        var maxScoreLookup = levelMaxScores.ToDictionary(x => x.LevelId, x => x.MaxScore);

        var result = new List<GameSectionDto>(sections.Count);

        foreach (var section in sections)
        {
            var sectionLevels = levels.Where(l => l.SectionId == section.Id).ToList();
            var totalLevels = sectionLevels.Count;
            var levelsCompleted = 0;
            var score = 0;
            var maxScore = 0;

            foreach (var level in sectionLevels)
            {
                if (progressLookup.TryGetValue(level.Id, out var lvlProgress))
                {
                    if (lvlProgress.Status == LevelProgressStatuses.Completed)
                    {
                        levelsCompleted++;
                    }

                    score += lvlProgress.BestScore;
                    maxScore += lvlProgress.MaxScore;
                }
                else
                {
                    if (maxScoreLookup.TryGetValue(level.Id, out var levelMax))
                    {
                        maxScore += levelMax;
                    }
                }
            }

            var dto = new GameSectionDto
            {
                Id = section.Id,
                Name = section.Name,
                ImageUrl = section.ImageUrl,
                OrderIndex = section.OrderIndex,
                LevelsCompleted = levelsCompleted,
                TotalLevels = totalLevels,
                Score = score,
                MaxScore = maxScore,
                IsCompleted = totalLevels > 0 && levelsCompleted == totalLevels
            };

            result.Add(dto);
        }

        return Ok(result);
    }

    [HttpGet("{sectionId:guid}/levels")]
    public async Task<ActionResult<IEnumerable<GameLevelDto>>> GetLevels(Guid sectionId, CancellationToken ct)
    {
        ApplyNoCacheHeaders();
        var userId = GetCurrentUserId();

        var targetSectionId = sectionId;

        var sectionExists = await _db.Sections
            .AnyAsync(s => s.Id == sectionId && s.Status == PublishStatus.Published, ct);

        if (!sectionExists)
        {
            // Some callers provide the level id instead of the section id; resolve it back to the section.
            var resolvedSectionId = await _db.Levels
                .Where(l => l.Id == sectionId && l.Status == PublishStatus.Published)
                .Select(l => (Guid?)l.SectionId)
                .FirstOrDefaultAsync(ct);

            if (resolvedSectionId is null)
            {
                return NotFound();
            }

            targetSectionId = resolvedSectionId.Value;
        }

        var levels = await _db.Levels
            .Where(l => l.SectionId == targetSectionId && l.Status == PublishStatus.Published)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Name)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.ImageUrl,
                l.HeaderText,
                l.OrderIndex
            })
            .ToListAsync(ct);

        if (levels.Count == 0)
        {
            return Ok(Array.Empty<GameLevelDto>());
        }

        var levelIds = levels.Select(l => l.Id).ToList();

        var progress = await _db.UserLevelProgress
            .Where(p => p.UserId == userId && levelIds.Contains(p.LevelId))
            .ToListAsync(ct);

        var maxScores = await _db.Tasks
            .Where(t => levelIds.Contains(t.LevelId) && t.Status == PublishStatus.Published)
            .GroupBy(t => t.LevelId)
            .Select(g => new { LevelId = g.Key, MaxScore = g.Sum(t => t.PointsAttempt1) })
            .ToListAsync(ct);

        var progressLookup = progress.ToDictionary(p => p.LevelId, p => p);
        var maxLookup = maxScores.ToDictionary(x => x.LevelId, x => x.MaxScore);

        var result = levels.Select(l =>
        {
            progressLookup.TryGetValue(l.Id, out var p);
            var maxScore = p?.MaxScore ?? (maxLookup.TryGetValue(l.Id, out var m) ? m : 0);

            return new GameLevelDto
            {
                Id = l.Id,
                Name = l.Name,
                ImageUrl = l.ImageUrl,
                HeaderText = l.HeaderText,
                OrderIndex = l.OrderIndex,
                Status = p?.Status ?? LevelProgressStatuses.NotStarted,
                IsCompleted = p?.Status == LevelProgressStatuses.Completed,
                Score = p?.BestScore ?? 0,
                MaxScore = maxScore
            };
        }).ToList();

        var lockingInputs = result.Select(l => new LevelLockingInfo
        {
            LevelId = l.Id,
            OrderIndex = l.OrderIndex,
            Status = l.Status,
            IsCompleted = l.IsCompleted
        }).ToList();

        var finalStatuses = LevelLockingService.ComputeStatuses(lockingInputs);
        foreach (var level in result)
        {
            if (finalStatuses.TryGetValue(level.Id, out var status))
            {
                level.Status = status;
                level.IsCompleted = status == LevelProgressStatuses.Completed;
            }
        }

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                          User.FindFirstValue("sub");
        if (userIdClaim == null)
        {
            throw new InvalidOperationException("User identifier claim is missing");
        }

        return Guid.Parse(userIdClaim);
    }

    private void ApplyNoCacheHeaders()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Vary"] = "Authorization";
    }
}
