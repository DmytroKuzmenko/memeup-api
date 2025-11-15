using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Game;
using Memeup.Api.Domain.Levels;
using Memeup.Api.Domain.Tasks;
using Memeup.Api.Features.Game;
using Microsoft.Extensions.Logging;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/game/levels")]
[Authorize]
public class GameLevelsController : ControllerBase
{
    private static readonly TimeSpan TimerGrace = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReplayCooldown = TimeSpan.FromSeconds(30);
    private readonly MemeupDbContext _db;
    private readonly ILogger<GameLevelsController> _logger;

    public GameLevelsController(MemeupDbContext db, ILogger<GameLevelsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("{levelId:guid}/intro")]
    public async Task<ActionResult<LevelIntroDto>> Intro(Guid levelId, CancellationToken ct)
    {
        ApplyNoCacheHeaders();
        var userId = GetCurrentUserId();

        var level = await _db.Levels
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == levelId && l.Status == PublishStatus.Published, ct);

        if (level == null)
        {
            return NotFound();
        }

        var statusLookup = await LevelLockingService.ComputeStatusesAsync(_db, level.SectionId, userId, ct);
        var levelStatus = statusLookup.TryGetValue(level.Id, out var resolvedStatus)
            ? resolvedStatus
            : LevelProgressStatuses.Locked;

        var tasksQuery = _db.Tasks
            .Where(t => t.LevelId == levelId && t.Status == PublishStatus.Published);

        var tasksCount = await tasksQuery.CountAsync(ct);
        var maxScore = await tasksQuery.SumAsync(t => (int?)t.PointsAttempt1, ct) ?? 0;

        var progress = await _db.UserLevelProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LevelId == levelId, ct);

        var dto = new LevelIntroDto
        {
            LevelId = level.Id,
            LevelName = level.Name,
            HeaderText = level.HeaderText,
            AnimationImageUrl = level.AnimationImageUrl,
            OrderIndex = level.OrderIndex,
            TasksCount = tasksCount,
            MaxScore = maxScore,
            Status = levelStatus,
            ReplayAvailableAt = progress?.ReplayAvailableAt
        };

        return Ok(dto);
    }

    [HttpPost("{levelId:guid}/start")]
    public async Task<ActionResult<TaskDeliveryResponse>> Start(Guid levelId, CancellationToken ct)
    {
        ApplyNoCacheHeaders();
        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;

        var level = await _db.Levels
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == levelId && l.Status == PublishStatus.Published, ct);

        if (level == null)
        {
            return NotFound();
        }

        var levelStatus = await ResolveLevelStatus(userId, level, ct);
        if (levelStatus == LevelProgressStatuses.Locked)
        {
            _logger.LogWarning("User {UserId} attempted to start locked level {LevelId}", userId, level.Id);
            return LevelLocked();
        }

        var tasks = await LoadOrderedTasks(levelId, ct);
        if (tasks.Count == 0)
        {
            return BadRequest(new { message = "Level has no published tasks" });
        }

        var maxScore = tasks.Sum(t => t.PointsAttempt1);

        var progress = await _db.UserLevelProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LevelId == levelId, ct);

        bool isNewRun = false;
        if (progress == null)
        {
            progress = new UserLevelProgress
            {
                UserId = userId,
                LevelId = levelId,
                Status = LevelProgressStatuses.InProgress,
                MaxScore = maxScore,
                RunsCount = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.UserLevelProgress.Add(progress);
            isNewRun = true;
        }
        else
        {
            if (progress.Status == LevelProgressStatuses.Completed)
            {
                if (progress.ReplayAvailableAt.HasValue && progress.ReplayAvailableAt.Value > now)
                {
                    var retryAfter = (int)Math.Ceiling((progress.ReplayAvailableAt.Value - now).TotalSeconds);
                    Response.Headers["Retry-After"] = retryAfter.ToString();
                    return StatusCode(429, new { message = "Replay cooldown active" });
                }

                await ResetLevelState(progress, userId, levelId, maxScore, tasks, now, ct, incrementRun: true);
                isNewRun = true;
            }
            else if (progress.Status == LevelProgressStatuses.NotStarted)
            {
                var shouldIncrement = progress.RunsCount == 0;
                await ResetLevelState(progress, userId, levelId, maxScore, tasks, now, ct, shouldIncrement);
                isNewRun = true;
            }
            else if (progress.Status == LevelProgressStatuses.Locked)
            {
                _logger.LogWarning("User {UserId} attempted to start locked level {LevelId}", userId, level.Id);
                return LevelLocked();
            }
            else
            {
                progress.MaxScore = maxScore;
            }
        }

        await _db.SaveChangesAsync(ct);

        var delivery = await DeliverNextTask(userId, level, tasks, progress, ct);

        if (isNewRun)
        {
            await _db.SaveChangesAsync(ct);
        }

        return Ok(delivery);
    }

    [HttpGet("{levelId:guid}/next")]
    public async Task<ActionResult<TaskDeliveryResponse>> Next(Guid levelId, CancellationToken ct)
    {
        ApplyNoCacheHeaders();
        var userId = GetCurrentUserId();

        var level = await _db.Levels
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == levelId && l.Status == PublishStatus.Published, ct);

        if (level == null)
        {
            return NotFound();
        }

        var levelStatus = await ResolveLevelStatus(userId, level, ct);
        if (levelStatus == LevelProgressStatuses.Locked)
        {
            _logger.LogWarning("User {UserId} attempted to fetch next task for locked level {LevelId}", userId, level.Id);
            return LevelLocked();
        }

        var tasks = await LoadOrderedTasks(levelId, ct);
        if (tasks.Count == 0)
        {
            return BadRequest(new { message = "Level has no published tasks" });
        }

        var maxScore = tasks.Sum(t => t.PointsAttempt1);
        var progress = await _db.UserLevelProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LevelId == levelId, ct);

        if (progress == null)
        {
            var now = DateTimeOffset.UtcNow;
            progress = new UserLevelProgress
            {
                UserId = userId,
                LevelId = levelId,
                Status = LevelProgressStatuses.InProgress,
                MaxScore = maxScore,
                RunsCount = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.UserLevelProgress.Add(progress);
            await _db.SaveChangesAsync(ct);
        }

        if (progress.Status == LevelProgressStatuses.Completed)
        {
            return Ok(new TaskDeliveryResponse
            {
                LevelId = level.Id,
                Status = "completed",
                LevelProgress = await BuildLevelProgress(userId, levelId, ct)
            });
        }

        var delivery = await DeliverNextTask(userId, level, tasks, progress, ct);
        await _db.SaveChangesAsync(ct);
        return Ok(delivery);
    }

    [HttpPost("{levelId:guid}/replay")]
    public async Task<ActionResult<TaskDeliveryResponse>> Replay(Guid levelId, CancellationToken ct)
    {
        ApplyNoCacheHeaders();
        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;

        var level = await _db.Levels
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == levelId && l.Status == PublishStatus.Published, ct);

        if (level == null)
        {
            return NotFound();
        }

        var levelStatus = await ResolveLevelStatus(userId, level, ct);
        if (levelStatus == LevelProgressStatuses.Locked)
        {
            _logger.LogWarning("User {UserId} attempted to replay locked level {LevelId}", userId, level.Id);
            return LevelLocked();
        }

        var progress = await _db.UserLevelProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LevelId == levelId, ct);

        if (progress == null || progress.Status != LevelProgressStatuses.Completed)
        {
            return BadRequest(new { message = "Level has not been completed yet" });
        }

        if (progress.ReplayAvailableAt.HasValue && progress.ReplayAvailableAt.Value > now)
        {
            var retryAfter = (int)Math.Ceiling((progress.ReplayAvailableAt.Value - now).TotalSeconds);
            Response.Headers["Retry-After"] = retryAfter.ToString();
            return StatusCode(429, new { message = "Replay cooldown active" });
        }

        var tasks = await LoadOrderedTasks(levelId, ct);
        if (tasks.Count == 0)
        {
            return BadRequest(new { message = "Level has no published tasks" });
        }

        await ResetLevelState(progress, userId, levelId, tasks.Sum(t => t.PointsAttempt1), tasks, now, ct, incrementRun: true);
        await _db.SaveChangesAsync(ct);

        var delivery = await DeliverNextTask(userId, level, tasks, progress, ct);
        await _db.SaveChangesAsync(ct);
        return Ok(delivery);
    }

    private async Task ResetLevelState(UserLevelProgress progress, Guid userId, Guid levelId, int maxScore, IReadOnlyList<TaskItem> tasks, DateTimeOffset now, CancellationToken ct, bool incrementRun)
    {
        var existingTaskProgress = await _db.UserTaskProgress
            .Where(p => p.UserId == userId && p.LevelId == levelId)
            .ToListAsync(ct);

        if (existingTaskProgress.Count > 0)
        {
            _db.UserTaskProgress.RemoveRange(existingTaskProgress);
        }

        progress.Status = LevelProgressStatuses.InProgress;
        progress.LastRunScore = 0;
        progress.MaxScore = maxScore;
        progress.LastTaskId = null;
        progress.LastCompletedAt = null;
        progress.ReplayAvailableAt = null;
        progress.UpdatedAt = now;
        if (incrementRun)
        {
            progress.RunsCount = Math.Max(progress.RunsCount, 0) + 1;
        }
        else if (progress.RunsCount == 0)
        {
            progress.RunsCount = 1;
        }
    }

    private async Task<List<TaskItem>> LoadOrderedTasks(Guid levelId, CancellationToken ct)
    {
        var tasks = await _db.Tasks
            .Where(t => t.LevelId == levelId && t.Status == PublishStatus.Published)
            .Include(t => t.Options)
            .ToListAsync(ct);

        return tasks
            .OrderBy(t => t.OrderIndex)
            .ThenBy(t => t.CreatedAt)
            .ToList();
    }

    private async Task<TaskDeliveryResponse> DeliverNextTask(Guid userId, Level level, IReadOnlyList<TaskItem> tasks, UserLevelProgress progress, CancellationToken ct)
    {
        var taskProgress = await _db.UserTaskProgress
            .Where(p => p.UserId == userId && p.LevelId == level.Id)
            .ToListAsync(ct);

        var progressLookup = taskProgress.ToDictionary(p => p.TaskId, p => p);

        var nextTask = ResolveNextTask(tasks, progress, progressLookup);
        if (nextTask == null)
        {
            var completedAt = DateTimeOffset.UtcNow;
            var score = taskProgress.Sum(tp => tp.PointsEarned);
            var maxScore = tasks.Sum(t => t.PointsAttempt1);

            progress.Status = LevelProgressStatuses.Completed;
            progress.LastRunScore = score;
            progress.MaxScore = maxScore;
            progress.BestScore = Math.Max(progress.BestScore, score);
            progress.LastTaskId = null;
            progress.LastCompletedAt = completedAt;
            progress.ReplayAvailableAt = completedAt.Add(ReplayCooldown);
            progress.UpdatedAt = completedAt;

            return new TaskDeliveryResponse
            {
                LevelId = level.Id,
                Status = "completed",
                Task = null,
                LevelProgress = await BuildLevelProgress(userId, level.Id, ct)
            };
        }

        if (!progressLookup.TryGetValue(nextTask.Id, out var currentTaskProgress))
        {
            currentTaskProgress = new UserTaskProgress
            {
                UserId = userId,
                LevelId = level.Id,
                TaskId = nextTask.Id,
                AttemptsUsed = 0,
                PointsEarned = 0,
                IsCompleted = false,
                TimeSpentSec = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.UserTaskProgress.Add(currentTaskProgress);
            progressLookup[nextTask.Id] = currentTaskProgress;
        }

        var maxAttempts = CalculateMaxAttempts(nextTask);
        if (currentTaskProgress.AttemptsUsed >= maxAttempts || currentTaskProgress.IsCompleted)
        {
            if (currentTaskProgress.AttemptsUsed >= maxAttempts && !currentTaskProgress.IsCompleted)
            {
                currentTaskProgress.IsCompleted = true;
            }
            // Move to the following task
            progress.LastTaskId = nextTask.Id;
            return await DeliverNextTask(userId, level, tasks, progress, ct);
        }

        var attemptNumber = currentTaskProgress.AttemptsUsed + 1;
        var timeLimit = nextTask.TimeLimitSec ?? level.TimeLimitSec;
        var now = DateTimeOffset.UtcNow;
        var expiresAt = timeLimit is int seconds && seconds > 0
            ? now.AddSeconds(seconds).Add(TimerGrace)
            : DateTimeOffset.MaxValue;

        await FinalizeActiveAttempts(userId, nextTask.Id, ct);

        var activeAttempt = new ActiveTaskAttempt
        {
            UserId = userId,
            LevelId = level.Id,
            TaskId = nextTask.Id,
            AttemptNumber = attemptNumber,
            AttemptStartAt = now,
            ExpiresAt = expiresAt,
            IsFinalized = false,
            Token = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ActiveTaskAttempts.Add(activeAttempt);

        progress.LastTaskId = nextTask.Id;
        progress.Status = LevelProgressStatuses.InProgress;
        progress.MaxScore = tasks.Sum(t => t.PointsAttempt1);
        progress.LastRunScore = progressLookup.Values.Sum(tp => tp.PointsEarned);

        var shuffledOptions = nextTask.Options
            .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
            .Select(o => new GameTaskOptionDto
            {
                Id = o.Id,
                Label = o.Label,
                ImageUrl = o.ImageUrl
            })
            .ToList();

        var response = new TaskDeliveryResponse
        {
            LevelId = level.Id,
            Status = "ok",
            Task = new GameTaskDto
            {
                Id = nextTask.Id,
                Type = nextTask.Type.ToString(),
                HeaderText = nextTask.HeaderText,
                ImageUrl = nextTask.ImageUrl,
                ResultImagePath = nextTask.ResultImagePath,
                ResultImageSource = nextTask.ResultImageSource,
                TaskImageSource = nextTask.TaskImageSource,
                Options = shuffledOptions,
                OrderIndex = nextTask.OrderIndex,
                TimeLimitSecEffective = timeLimit,
                AttemptToken = activeAttempt.Token
            },
            LevelProgress = await BuildLevelProgress(userId, level.Id, ct)
        };

        return response;
    }

    private TaskItem? ResolveNextTask(IReadOnlyList<TaskItem> tasks, UserLevelProgress progress, IDictionary<Guid, UserTaskProgress> taskProgress)
    {
        if (tasks.Count == 0)
        {
            return null;
        }

        if (progress.LastTaskId.HasValue)
        {
            var current = tasks.FirstOrDefault(t => t.Id == progress.LastTaskId.Value);
            if (current != null)
            {
                if (!taskProgress.TryGetValue(current.Id, out var currentProgress) || !currentProgress.IsCompleted)
                {
                    return current;
                }

                var currentIndex = -1;
                for (var i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i].Id == current.Id)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                if (currentIndex < 0)
                {
                    return current;
                }

                for (var i = currentIndex + 1; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    if (!taskProgress.TryGetValue(task.Id, out var tp) || !tp.IsCompleted)
                    {
                        return task;
                    }
                }

                return null;
            }
        }

        foreach (var task in tasks)
        {
            if (!taskProgress.TryGetValue(task.Id, out var tp) || !tp.IsCompleted)
            {
                return task;
            }
        }

        return null;
    }

    private async Task FinalizeActiveAttempts(Guid userId, Guid taskId, CancellationToken ct)
    {
        var activeAttempts = await _db.ActiveTaskAttempts
            .Where(a => a.UserId == userId && a.TaskId == taskId && !a.IsFinalized)
            .ToListAsync(ct);

        if (activeAttempts.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var attempt in activeAttempts)
        {
            attempt.IsFinalized = true;
            attempt.UpdatedAt = now;
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

    private async Task<string> ResolveLevelStatus(Guid userId, Level level, CancellationToken ct)
    {
        var statusLookup = await LevelLockingService.ComputeStatusesAsync(_db, level.SectionId, userId, ct);
        if (statusLookup.TryGetValue(level.Id, out var status))
        {
            return status;
        }

        return LevelProgressStatuses.Locked;
    }

    private ActionResult<TaskDeliveryResponse> LevelLocked()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            error = "LevelLocked",
            message = "Previous levels must be completed first."
        });
    }

    private async Task<LevelProgressDto> BuildLevelProgress(Guid userId, Guid levelId, CancellationToken ct)
    {
        var tasks = await _db.Tasks
            .Where(t => t.LevelId == levelId && t.Status == PublishStatus.Published)
            .Select(t => new { t.Id, t.PointsAttempt1 })
            .ToListAsync(ct);

        var progress = await _db.UserTaskProgress
            .Where(p => p.UserId == userId && p.LevelId == levelId)
            .ToListAsync(ct);

        var completed = progress.Count(p => p.IsCompleted);
        var score = progress.Sum(p => p.PointsEarned);
        var maxScore = tasks.Sum(t => t.PointsAttempt1);

        return new LevelProgressDto
        {
            CompletedTasks = completed,
            TotalTasks = tasks.Count,
            Score = score,
            MaxScore = maxScore
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

    private void ApplyNoCacheHeaders()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Vary"] = "Authorization";
    }
}
