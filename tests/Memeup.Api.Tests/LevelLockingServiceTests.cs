using System;
using System.Linq;
using Memeup.Api.Domain.Game;
using Memeup.Api.Features.Game;
using Xunit;

namespace Memeup.Api.Tests;

public class LevelLockingServiceTests
{
    [Fact]
    public void ComputeStatuses_LocksAllButFirstOpenLevel()
    {
        var level1 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 0,
            Status = LevelProgressStatuses.InProgress,
            IsCompleted = false
        };
        var level2 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 1,
            Status = LevelProgressStatuses.NotStarted,
            IsCompleted = false
        };
        var level3 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 2,
            Status = LevelProgressStatuses.NotStarted,
            IsCompleted = false
        };

        var statuses = LevelLockingService.ComputeStatuses(new[] { level1, level2, level3 });

        Assert.Equal(LevelProgressStatuses.InProgress, statuses[level1.LevelId]);
        Assert.Equal(LevelProgressStatuses.Locked, statuses[level2.LevelId]);
        Assert.Equal(LevelProgressStatuses.Locked, statuses[level3.LevelId]);
    }

    [Fact]
    public void ComputeStatuses_AllCompletedRemainCompleted()
    {
        var levels = Enumerable.Range(0, 3)
            .Select(_ => new LevelLockingInfo
            {
                LevelId = Guid.NewGuid(),
                OrderIndex = _,
                Status = LevelProgressStatuses.Completed,
                IsCompleted = true
            })
            .ToList();

        var statuses = LevelLockingService.ComputeStatuses(levels);

        Assert.All(statuses.Values, status => Assert.Equal(LevelProgressStatuses.Completed, status));
    }

    [Fact]
    public void ComputeStatuses_OnlySecondLevelRemainsOpen()
    {
        var level1 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 0,
            Status = LevelProgressStatuses.Completed,
            IsCompleted = true
        };
        var level2 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 1,
            Status = LevelProgressStatuses.NotStarted,
            IsCompleted = false
        };
        var level3 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 2,
            Status = LevelProgressStatuses.NotStarted,
            IsCompleted = false
        };

        var statuses = LevelLockingService.ComputeStatuses(new[] { level1, level2, level3 });

        Assert.Equal(LevelProgressStatuses.Completed, statuses[level1.LevelId]);
        Assert.Equal(LevelProgressStatuses.NotStarted, statuses[level2.LevelId]);
        Assert.Equal(LevelProgressStatuses.Locked, statuses[level3.LevelId]);
    }

    [Fact]
    public void ComputeStatuses_HandlesGapsInOrderIndex()
    {
        var level1 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 0,
            Status = LevelProgressStatuses.Completed,
            IsCompleted = true
        };
        var level2 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 5,
            Status = LevelProgressStatuses.InProgress,
            IsCompleted = false
        };
        var level3 = new LevelLockingInfo
        {
            LevelId = Guid.NewGuid(),
            OrderIndex = 10,
            Status = LevelProgressStatuses.NotStarted,
            IsCompleted = false
        };

        var statuses = LevelLockingService.ComputeStatuses(new[] { level3, level2, level1 });

        Assert.Equal(LevelProgressStatuses.Completed, statuses[level1.LevelId]);
        Assert.Equal(LevelProgressStatuses.InProgress, statuses[level2.LevelId]);
        Assert.Equal(LevelProgressStatuses.Locked, statuses[level3.LevelId]);
    }
}
