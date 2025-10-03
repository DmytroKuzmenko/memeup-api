using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Controllers;
using Memeup.Api.Data;
using Memeup.Api.Domain.Levels;
using Memeup.Api.Domain.Sections;
using Memeup.Api.Features.Tasks;
using Xunit;

namespace Memeup.Api.Tests;

public class TasksControllerTests
{
    [Fact]
    public async Task CreateAndUpdate_PersistsTaskOptions()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MemeupDbContext>()
            .UseSqlite(connection)
            .Options;

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new TaskMappingProfile()))
            .CreateMapper();

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = "Section"
        };
        var level = new Level
        {
            Id = Guid.NewGuid(),
            Name = "Level",
            SectionId = section.Id,
            Section = section
        };

        await using (var setup = new MemeupDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Sections.Add(section);
            setup.Levels.Add(level);
            await setup.SaveChangesAsync();
        }

        await using var context = new MemeupDbContext(options);
        var controller = new TasksController(context, mapper);

        var createDto = new TaskCreateDto
        {
            LevelId = level.Id,
            Status = 0,
            Type = 3,
            InternalName = "initial",
            HeaderText = "header",
            ImageUrl = "image",
            Options =
            [
                new TaskOptionDto { Label = "Option A", IsCorrect = true },
                new TaskOptionDto { Label = "Option B", IsCorrect = false }
            ],
            OrderIndex = 1,
            TimeLimitSec = 30,
            PointsAttempt1 = 10,
            PointsAttempt2 = 5,
            PointsAttempt3 = 1,
            ExplanationText = "explanation"
        };

        var createResult = await controller.Create(createDto);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdDto = Assert.IsType<TaskDto>(created.Value);
        Assert.Equal(2, createdDto.Options.Length);
        var updateDto = new TaskUpdateDto
        {
            Status = 0,
            Type = 3,
            InternalName = "updated",
            HeaderText = "updated header",
            ImageUrl = "updated-image",
            Options =
            [
                new TaskOptionDto { Label = "Updated Option", IsCorrect = true, ImageUrl = "option.png" }
            ],
            OrderIndex = 2,
            TimeLimitSec = 45,
            PointsAttempt1 = 12,
            PointsAttempt2 = 6,
            PointsAttempt3 = 2,
            ExplanationText = "updated explanation"
        };

        var updateResult = await controller.Update(createdDto.Id, updateDto);
        var ok = Assert.IsType<OkObjectResult>(updateResult.Result);
        var updatedDto = Assert.IsType<TaskDto>(ok.Value);

        Assert.Single(updatedDto.Options);
        Assert.Equal("Updated Option", updatedDto.Options[0].Label);
        Assert.Equal("option.png", updatedDto.Options[0].ImageUrl);
        Assert.True(updatedDto.Options[0].IsCorrect);
        var entity = await context.Tasks
            .Include(t => t.Options)
            .SingleAsync(t => t.Id == createdDto.Id);

        Assert.Single(entity.Options);
        Assert.Equal("Updated Option", entity.Options.First().Label);
        Assert.Equal("option.png", entity.Options.First().ImageUrl);
        Assert.True(entity.Options.First().IsCorrect);
    }

}
}
