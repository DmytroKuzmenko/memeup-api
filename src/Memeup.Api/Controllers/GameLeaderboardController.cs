using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Game;
using Memeup.Api.Features.Game;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/game/leaderboard")]
[Authorize]
public class GameLeaderboardController : ControllerBase
{
    private readonly MemeupDbContext _db;

    public GameLeaderboardController(MemeupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get([FromQuery] string? period, [FromQuery] Guid? sectionId, [FromQuery] Guid? levelId, CancellationToken ct)
    {
        ApplyNoCacheHeaders();
        var effectivePeriod = string.IsNullOrWhiteSpace(period) ? "AllTime" : period!;
        if (!string.Equals(effectivePeriod, "AllTime", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only AllTime period is supported" });
        }

        IQueryable<UserLevelProgress> levelProgress = _db.UserLevelProgress.AsQueryable();

        if (levelId.HasValue)
        {
            levelProgress = levelProgress.Where(p => p.LevelId == levelId.Value);
        }
        else if (sectionId.HasValue)
        {
            var sectionLevelIds = await _db.Levels
                .Where(l => l.SectionId == sectionId.Value)
                .Select(l => l.Id)
                .ToListAsync(ct);

            if (sectionLevelIds.Count == 0)
            {
                return Ok(Array.Empty<LeaderboardEntryDto>());
            }

            levelProgress = levelProgress.Where(p => sectionLevelIds.Contains(p.LevelId));
        }

        var aggregates = await levelProgress
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Score = g.Sum(x => x.BestScore) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.UserId)
            .Take(100)
            .ToListAsync(ct);

        if (aggregates.Count == 0)
        {
            return Ok(Array.Empty<LeaderboardEntryDto>());
        }

        var userIds = aggregates.Select(a => a.UserId).ToList();

        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.Email })
            .ToListAsync(ct);

        var userLookup = users.ToDictionary(u => u.Id, u => u);

        var leaderboardEntries = await _db.LeaderboardEntries
            .Where(e => e.Period == "AllTime" && userIds.Contains(e.UserId))
            .ToListAsync(ct);

        var entryLookup = leaderboardEntries.ToDictionary(e => e.UserId, e => e);

        var results = new List<LeaderboardEntryDto>(aggregates.Count);
        var rank = 1;
        foreach (var aggregate in aggregates)
        {
            userLookup.TryGetValue(aggregate.UserId, out var user);
            entryLookup.TryGetValue(aggregate.UserId, out var leaderboardEntry);

            var displayName = user?.UserName ?? user?.Email ?? "Player";
            var updatedAt = leaderboardEntry?.UpdatedAt ?? DateTimeOffset.UtcNow;

            results.Add(new LeaderboardEntryDto
            {
                UserId = aggregate.UserId,
                DisplayName = displayName,
                Score = aggregate.Score,
                Rank = rank++,
                UpdatedAt = updatedAt
            });
        }

        return Ok(results);
    }

    private void ApplyNoCacheHeaders()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Vary"] = "Authorization";
    }
}
