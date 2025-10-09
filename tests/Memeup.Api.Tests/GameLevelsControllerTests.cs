using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Memeup.Api.Controllers;
using Memeup.Api.Data;
using Memeup.Api.Domain.Enums;
using Memeup.Api.Domain.Levels;
using Memeup.Api.Domain.Sections;
using Xunit;

namespace Memeup.Api.Tests;

public class GameLevelsControllerTests
{
    [Fact]
    public async Task Start_ReturnsForbiddenWhenLevelLocked()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MemeupDbContext>()
            .UseSqlite(connection)
            .Options;

        var userId = Guid.NewGuid();
        var section = new Section { Id = Guid.NewGuid(), Name = "Section" };
        var level1 = new Level
        {
            Id = Guid.NewGuid(),
            SectionId = section.Id,
            Section = section,
            Name = "Level 1",
            OrderIndex = 0,
            Status = PublishStatus.Published
        };
        var level2 = new Level
        {
            Id = Guid.NewGuid(),
            SectionId = section.Id,
            Section = section,
            Name = "Level 2",
            OrderIndex = 1,
            Status = PublishStatus.Published
        };

        await using (var setup = new MemeupDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Sections.Add(section);
            setup.Levels.AddRange(level1, level2);
            await setup.SaveChangesAsync();
        }

        await using var context = new MemeupDbContext(options);
        var controller = new GameLevelsController(context, NullLogger<GameLevelsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "test"))
                }
            }
        };

        var response = await controller.Start(level2.Id, CancellationToken.None);
        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal("LevelLocked", GetPropertyValue<string>(result.Value, "error"));
        Assert.Equal("Previous levels must be completed first.", GetPropertyValue<string>(result.Value, "message"));
    }

    [Fact]
    public async Task Next_ReturnsForbiddenWhenLevelLocked()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MemeupDbContext>()
            .UseSqlite(connection)
            .Options;

        var userId = Guid.NewGuid();
        var section = new Section { Id = Guid.NewGuid(), Name = "Section" };
        var level1 = new Level
        {
            Id = Guid.NewGuid(),
            SectionId = section.Id,
            Section = section,
            Name = "Level 1",
            OrderIndex = 0,
            Status = PublishStatus.Published
        };
        var level2 = new Level
        {
            Id = Guid.NewGuid(),
            SectionId = section.Id,
            Section = section,
            Name = "Level 2",
            OrderIndex = 1,
            Status = PublishStatus.Published
        };

        await using (var setup = new MemeupDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Sections.Add(section);
            setup.Levels.AddRange(level1, level2);
            await setup.SaveChangesAsync();
        }

        await using var context = new MemeupDbContext(options);
        var controller = new GameLevelsController(context, NullLogger<GameLevelsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "test"))
                }
            }
        };

        var response = await controller.Next(level2.Id, CancellationToken.None);
        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal("LevelLocked", GetPropertyValue<string>(result.Value, "error"));
        Assert.Equal("Previous levels must be completed first.", GetPropertyValue<string>(result.Value, "message"));
    }

    private static T? GetPropertyValue<T>(object? instance, string propertyName)
    {
        if (instance == null)
        {
            return default;
        }

        var property = instance.GetType().GetProperty(propertyName);
        if (property == null)
        {
            return default;
        }

        return (T?)property.GetValue(instance);
    }
}
