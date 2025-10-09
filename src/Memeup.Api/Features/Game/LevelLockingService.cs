using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Game;
using Microsoft.EntityFrameworkCore;

namespace Memeup.Api.Features.Game;

public class LevelLockingInfo
{
    public Guid LevelId { get; set; }
    public int OrderIndex { get; set; }
    public string Status { get; set; } = LevelProgressStatuses.NotStarted;
    public bool IsCompleted { get; set; }
}

public static class LevelLockingService
{
    private static readonly IReadOnlyDictionary<Guid, string> EmptyStatuses = new Dictionary<Guid, string>();

    public static IReadOnlyDictionary<Guid, string> ComputeStatuses(IReadOnlyList<LevelLockingInfo> levels)
    {
        if (levels == null)
        {
            throw new ArgumentNullException(nameof(levels));
        }

        if (levels.Count == 0)
        {
            return EmptyStatuses;
        }

        var ordered = levels
            .Select((level, index) => new { Level = level, OriginalIndex = index })
            .OrderBy(x => x.Level.OrderIndex)
            .ThenBy(x => x.Level.LevelId)
            .ToList();

        var firstOpenIndex = -1;
        for (var i = 0; i < ordered.Count; i++)
        {
            var level = ordered[i].Level;
            var status = NormalizeStatus(level.Status);
            var isCompleted = level.IsCompleted || status == LevelProgressStatuses.Completed;
            if (!isCompleted)
            {
                firstOpenIndex = i;
                break;
            }
        }

        var result = new Dictionary<Guid, string>(ordered.Count);

        for (var i = 0; i < ordered.Count; i++)
        {
            var level = ordered[i].Level;
            var status = NormalizeStatus(level.Status);
            var isCompleted = level.IsCompleted || status == LevelProgressStatuses.Completed;
            string finalStatus;

            if (isCompleted)
            {
                finalStatus = LevelProgressStatuses.Completed;
            }
            else if (firstOpenIndex >= 0 && i == firstOpenIndex)
            {
                finalStatus = status == LevelProgressStatuses.InProgress
                    ? LevelProgressStatuses.InProgress
                    : LevelProgressStatuses.NotStarted;
            }
            else if (firstOpenIndex >= 0 && i > firstOpenIndex)
            {
                finalStatus = LevelProgressStatuses.Locked;
            }
            else
            {
                finalStatus = status;
            }

            result[level.LevelId] = finalStatus;
        }

        return result;
    }

    public static async Task<IReadOnlyDictionary<Guid, string>> ComputeStatusesAsync(
        MemeupDbContext db,
        Guid sectionId,
        Guid userId,
        CancellationToken ct)
    {
        if (db == null)
        {
            throw new ArgumentNullException(nameof(db));
        }

        var levels = await db.Levels
            .AsNoTracking()
            .Where(l => l.SectionId == sectionId && l.Status == PublishStatus.Published)
            .Select(l => new LevelLockingInfo
            {
                LevelId = l.Id,
                OrderIndex = l.OrderIndex
            })
            .ToListAsync(ct);

        if (levels.Count == 0)
        {
            return EmptyStatuses;
        }

        var levelIds = levels.Select(l => l.LevelId).ToList();

        var progressLookup = await db.UserLevelProgress
            .AsNoTracking()
            .Where(p => p.UserId == userId && levelIds.Contains(p.LevelId))
            .Select(p => new { p.LevelId, p.Status })
            .ToDictionaryAsync(p => p.LevelId, p => p.Status ?? LevelProgressStatuses.NotStarted, ct);

        foreach (var level in levels)
        {
            if (progressLookup.TryGetValue(level.LevelId, out var status))
            {
                level.Status = NormalizeStatus(status);
                level.IsCompleted = level.Status == LevelProgressStatuses.Completed;
            }
            else
            {
                level.Status = LevelProgressStatuses.NotStarted;
                level.IsCompleted = false;
            }
        }

        return ComputeStatuses(levels);
    }

    private static string NormalizeStatus(string? status)
    {
        return status switch
        {
            LevelProgressStatuses.InProgress => LevelProgressStatuses.InProgress,
            LevelProgressStatuses.Completed => LevelProgressStatuses.Completed,
            LevelProgressStatuses.Locked => LevelProgressStatuses.Locked,
            _ => LevelProgressStatuses.NotStarted
        };
    }
}
