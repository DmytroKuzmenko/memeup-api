using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Game;
using Memeup.Api.Domain.Tasks;
using Memeup.Api.Features.Game;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/game/tasks")]
[Authorize]
public class GameTasksController : ControllerBase
{
    private readonly MemeupDbContext _db;

    public GameTasksController(MemeupDbContext db)
    {
        _db = db;
    }

    [HttpPost("{taskId:guid}/submit")]
    public async Task<ActionResult<TaskSubmitResponse>> Submit(Guid taskId, [FromBody] TaskSubmitRequest request, CancellationToken ct)
    {
        ApplyNoCacheHeaders();
        if (request == null)
        {
            return BadRequest();
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        async Task<ActionResult<TaskSubmitResponse>> ExecuteCore()
        {
            var userId = GetCurrentUserId();
            var now = DateTimeOffset.UtcNow;

            var activeAttempt = await _db.ActiveTaskAttempts
                .FirstOrDefaultAsync(a => a.Token == request.AttemptToken, ct);

            if (activeAttempt == null)
            {
                return NotFound(new { message = "Attempt token not found" });
            }

            if (activeAttempt.UserId != userId)
            {
                return Forbid();
            }

            if (activeAttempt.TaskId != taskId)
            {
                return BadRequest(new { message = "Attempt token does not match task" });
            }

            if (activeAttempt.IsFinalized)
            {
                return Conflict(new { message = "Attempt already finalized" });
            }

            var task = await _db.Tasks
                .Include(t => t.Level)
                .ThenInclude(l => l.Section)
                .Include(t => t.Options)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.Status == PublishStatus.Published, ct);

            if (task == null)
            {
                return NotFound();
            }

            if (activeAttempt.LevelId != task.LevelId)
            {
                return BadRequest(new { message = "Attempt token does not match level" });
            }

            var maxAttempts = CalculateMaxAttempts(task);
            var attemptNumber = Math.Min(activeAttempt.AttemptNumber, maxAttempts);

            var expired = activeAttempt.ExpiresAt != DateTimeOffset.MaxValue && now > activeAttempt.ExpiresAt;
            var timeSpent = (int)Math.Max(0, Math.Round((now - activeAttempt.AttemptStartAt).TotalSeconds));

            TaskOption? selectedOption = null;
            if (request.SelectedOptionId.HasValue)
            {
                selectedOption = task.Options.FirstOrDefault(o => o.Id == request.SelectedOptionId.Value);
                if (selectedOption == null)
                {
                    return UnprocessableEntity(new { message = "Selected option is invalid" });
                }
            }

            var isTimeout = expired;
            var isCorrect = !isTimeout && selectedOption != null && selectedOption.IsCorrect;
            if (!isTimeout && selectedOption == null)
            {
                return UnprocessableEntity(new { message = "Option must be provided" });
            }

            var points = isCorrect ? GetPointsForAttempt(task, attemptNumber) : 0;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var taskProgress = await _db.UserTaskProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.TaskId == task.Id, ct);

            if (taskProgress == null)
            {
                taskProgress = new UserTaskProgress
                {
                    UserId = userId,
                    LevelId = task.LevelId,
                    TaskId = task.Id,
                    AttemptsUsed = 0,
                    PointsEarned = 0,
                    IsCompleted = false,
                    TimeSpentSec = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.UserTaskProgress.Add(taskProgress);
            }

            taskProgress.AttemptsUsed = Math.Max(taskProgress.AttemptsUsed, attemptNumber);
            taskProgress.UpdatedAt = now;

            if (isCorrect)
            {
                taskProgress.PointsEarned = points;
                taskProgress.IsCompleted = true;
                taskProgress.CompletedAt = now;
                taskProgress.TimeSpentSec = timeSpent;
            }
            else if (isTimeout)
            {
                if (attemptNumber >= maxAttempts)
                {
                    taskProgress.IsCompleted = true;
                    taskProgress.CompletedAt = now;
                }
            }
            else
            {
                if (attemptNumber >= maxAttempts)
                {
                    taskProgress.IsCompleted = true;
                    taskProgress.CompletedAt = now;
                }
            }

            var log = new TaskAttemptLog
            {
                UserId = userId,
                LevelId = task.LevelId,
                TaskId = task.Id,
                AttemptNumber = attemptNumber,
                IsCorrect = isCorrect,
                IsTimeout = isTimeout,
                TimeSpentSec = timeSpent,
                StartedAt = activeAttempt.AttemptStartAt,
                SubmittedAt = now,
                PointsAwarded = points,
                ShownExplanation = isCorrect,
                ClientAgent = Request.Headers.UserAgent.ToString(),
                ClientTz = Request.Headers.TryGetValue("X-Timezone", out var tz) ? tz.ToString() : null,
                IpHash = null,
                CreatedAt = now
            };
            _db.TaskAttemptLogs.Add(log);

            activeAttempt.IsFinalized = true;
            activeAttempt.UpdatedAt = now;
            activeAttempt.ExpiresAt = now;

            var levelProgress = await _db.UserLevelProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LevelId == task.LevelId, ct);

            if (levelProgress == null)
            {
                levelProgress = new UserLevelProgress
                {
                    UserId = userId,
                    LevelId = task.LevelId,
                    Status = LevelProgressStatuses.InProgress,
                    LastRunScore = 0,
                    BestScore = 0,
                    MaxScore = 0,
                    RunsCount = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.UserLevelProgress.Add(levelProgress);
            }

            var allTaskProgress = await _db.UserTaskProgress
                .Where(p => p.UserId == userId && p.LevelId == task.LevelId)
                .ToListAsync(ct);

            var levelTasks = await _db.Tasks
                .Where(t => t.LevelId == task.LevelId && t.Status == PublishStatus.Published)
                .Select(t => new { t.Id, t.PointsAttempt1 })
                .ToListAsync(ct);

            var score = allTaskProgress.Sum(p => p.PointsEarned);
            var completedTasks = allTaskProgress.Count(p => p.IsCompleted);
            var totalTasks = levelTasks.Count;
            var maxScore = levelTasks.Sum(t => t.PointsAttempt1);

            levelProgress.LastRunScore = score;
            levelProgress.MaxScore = maxScore;
            levelProgress.UpdatedAt = now;
            levelProgress.LastTaskId = task.Id;

            var levelCompleted = completedTasks >= totalTasks && totalTasks > 0 && allTaskProgress.All(p => p.IsCompleted);
            LevelSummaryDto? summary = null;

            if (levelCompleted)
            {
                levelProgress.Status = LevelProgressStatuses.Completed;
                levelProgress.LastCompletedAt = now;
                levelProgress.ReplayAvailableAt = now.Add(ReplayCooldown);
                levelProgress.LastTaskId = null;
                levelProgress.BestScore = Math.Max(levelProgress.BestScore, score);
                summary = new LevelSummaryDto
                {
                    EarnedScore = score,
                    MaxScore = maxScore
                };

                await UpdateLeaderboard(userId, ct);
                await UpdateSectionProgress(userId, task.Level.SectionId, ct);
            }
            else
            {
                levelProgress.Status = LevelProgressStatuses.InProgress;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var response = new TaskSubmitResponse
            {
                Result = isTimeout ? "timeout" : isCorrect ? "correct" : "incorrect",
                AttemptNumber = attemptNumber,
                AttemptsLeft = Math.Max(0, maxAttempts - attemptNumber),
                PointsEarned = points,
                TaskCompleted = taskProgress.IsCompleted,
                LevelCompleted = levelCompleted,
                LevelSummary = summary,
                NextAction = "nextTask",
                ExplanationText = isCorrect ? task.ExplanationText : null,
                ResultImagePath = task.ResultImagePath,
                ResultImageSource = task.ResultImageSource,
                TaskImageSource = task.TaskImageSource
            };

            return Ok(response);
        }

        return await strategy.ExecuteAsync(ExecuteCore);
    }

    private async Task UpdateLeaderboard(Guid userId, CancellationToken ct)
    {
        var score = await _db.UserLevelProgress
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.BestScore, ct);

        var entry = await _db.LeaderboardEntries
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Period == "AllTime", ct);

        var now = DateTimeOffset.UtcNow;
        if (entry == null)
        {
            entry = new LeaderboardEntry
            {
                UserId = userId,
                Period = "AllTime",
                Score = score,
                UpdatedAt = now
            };
            _db.LeaderboardEntries.Add(entry);
        }
        else
        {
            entry.Score = score;
            entry.UpdatedAt = now;
        }
    }

    private async Task UpdateSectionProgress(Guid userId, Guid sectionId, CancellationToken ct)
    {
        var sectionLevels = await _db.Levels
            .Where(l => l.SectionId == sectionId && l.Status == PublishStatus.Published)
            .Select(l => l.Id)
            .ToListAsync(ct);

        if (sectionLevels.Count == 0)
        {
            return;
        }

        var levelProgress = await _db.UserLevelProgress
            .Where(p => p.UserId == userId && sectionLevels.Contains(p.LevelId))
            .ToListAsync(ct);

        var sectionTasks = await _db.Tasks
            .Where(t => sectionLevels.Contains(t.LevelId) && t.Status == PublishStatus.Published)
            .GroupBy(t => t.LevelId)
            .Select(g => new { g.Key, MaxScore = g.Sum(t => t.PointsAttempt1) })
            .ToListAsync(ct);

        var maxScore = sectionTasks.Sum(x => x.MaxScore);
        var score = levelProgress.Sum(p => p.BestScore);
        var levelsCompleted = levelProgress.Count(p => p.Status == LevelProgressStatuses.Completed);
        var totalLevels = sectionLevels.Count;

        var sectionProgress = await _db.UserSectionProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SectionId == sectionId, ct);

        if (sectionProgress == null)
        {
            sectionProgress = new UserSectionProgress
            {
                UserId = userId,
                SectionId = sectionId,
                LevelsCompleted = levelsCompleted,
                TotalLevels = totalLevels,
                Score = score,
                MaxScore = maxScore,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.UserSectionProgress.Add(sectionProgress);
        }
        else
        {
            sectionProgress.LevelsCompleted = levelsCompleted;
            sectionProgress.TotalLevels = totalLevels;
            sectionProgress.Score = score;
            sectionProgress.MaxScore = maxScore;
            sectionProgress.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private int CalculateMaxAttempts(TaskItem task)
    {
        var attempts = 1;
        if (task.PointsAttempt2 > 0)
        {
            attempts++;
        }

        if (task.PointsAttempt3 > 0)
        {
            attempts++;
        }

        return attempts;
    }

    private int GetPointsForAttempt(TaskItem task, int attemptNumber)
    {
        return attemptNumber switch
        {
            1 => task.PointsAttempt1,
            2 => task.PointsAttempt2,
            3 => task.PointsAttempt3,
            _ => 0
        };
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim == null)
        {
            throw new InvalidOperationException("User identifier claim is missing");
        }

        return Guid.Parse(userIdClaim);
    }

    private static TimeSpan ReplayCooldown => TimeSpan.FromSeconds(30);

    private void ApplyNoCacheHeaders()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Vary"] = "Authorization";
    }
}
