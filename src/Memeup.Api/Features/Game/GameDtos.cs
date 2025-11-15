using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Memeup.Api.Domain.Game;

namespace Memeup.Api.Features.Game;

public class GameSectionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("orderIndex")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("isCompleted")]
    public bool IsCompleted { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }

    [JsonPropertyName("levelsCompleted")]
    public int LevelsCompleted { get; set; }

    [JsonPropertyName("totalLevels")]
    public int TotalLevels { get; set; }
}

public class GameLevelDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("headerText")]
    public string? HeaderText { get; set; }

    [JsonPropertyName("orderIndex")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("isCompleted")]
    public bool IsCompleted { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = LevelProgressStatuses.NotStarted;
}

public class LevelIntroDto
{
    [JsonPropertyName("levelId")]
    public Guid LevelId { get; set; }

    [JsonPropertyName("levelName")]
    public string LevelName { get; set; } = string.Empty;

    [JsonPropertyName("headerText")]
    public string? HeaderText { get; set; }

    [JsonPropertyName("animationImageUrl")]
    public string? AnimationImageUrl { get; set; }

    [JsonPropertyName("orderIndex")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("tasksCount")]
    public int TasksCount { get; set; }

    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = LevelProgressStatuses.NotStarted;

    [JsonPropertyName("replayAvailableAt")]
    public DateTimeOffset? ReplayAvailableAt { get; set; }
}

public class GameTaskOptionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
}

public class LevelProgressDto
{
    [JsonPropertyName("completedTasks")]
    public int CompletedTasks { get; set; }

    [JsonPropertyName("totalTasks")]
    public int TotalTasks { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }
}

public class GameTaskDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("headerText")]
    public string? HeaderText { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("resultImagePath")]
    public string? ResultImagePath { get; set; }

    [JsonPropertyName("resultImageSource")]
    public string? ResultImageSource { get; set; }

    [JsonPropertyName("taskImageSource")]
    public string? TaskImageSource { get; set; }

    [JsonPropertyName("options")]
    public IReadOnlyList<GameTaskOptionDto> Options { get; set; } = Array.Empty<GameTaskOptionDto>();

    [JsonPropertyName("orderIndex")]
    public int OrderIndex { get; set; }

    [JsonPropertyName("timeLimitSecEffective")]
    public int? TimeLimitSecEffective { get; set; }

    [JsonPropertyName("attemptToken")]
    public Guid AttemptToken { get; set; }
}

public class TaskDeliveryResponse
{
    [JsonPropertyName("levelId")]
    public Guid LevelId { get; set; }

    [JsonPropertyName("task")]
    public GameTaskDto? Task { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok";

    [JsonPropertyName("levelProgress")]
    public LevelProgressDto LevelProgress { get; set; } = new();
}

public class TaskSubmitSelectionDto
{
    [JsonPropertyName("selectedOptionId")]
    public Guid SelectedOptionId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class TaskSubmitRequest
{
    [JsonPropertyName("selectedOptionIds")]
    public IReadOnlyCollection<Guid> SelectedOptionIds { get; set; } = Array.Empty<Guid>();

    [JsonPropertyName("selectedOptions")]
    public IReadOnlyCollection<TaskSubmitSelectionDto> SelectedOptions { get; set; } = Array.Empty<TaskSubmitSelectionDto>();

    [JsonPropertyName("attemptToken")]
    public Guid AttemptToken { get; set; }
}

public class LevelSummaryDto
{
    [JsonPropertyName("earnedScore")]
    public int EarnedScore { get; set; }

    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }
}

public class TaskSubmitResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("attemptNumber")]
    public int AttemptNumber { get; set; }

    [JsonPropertyName("attemptsLeft")]
    public int AttemptsLeft { get; set; }

    [JsonPropertyName("pointsEarned")]
    public int PointsEarned { get; set; }

    [JsonPropertyName("taskCompleted")]
    public bool TaskCompleted { get; set; }

    [JsonPropertyName("levelCompleted")]
    public bool LevelCompleted { get; set; }

    [JsonPropertyName("levelSummary")]
    public LevelSummaryDto? LevelSummary { get; set; }

    [JsonPropertyName("nextAction")]
    public string NextAction { get; set; } = "nextTask";

    [JsonPropertyName("explanationText")]
    public string? ExplanationText { get; set; }

    [JsonPropertyName("resultImagePath")]
    public string? ResultImagePath { get; set; }

    [JsonPropertyName("resultImageSource")]
    public string? ResultImageSource { get; set; }

    [JsonPropertyName("taskImageSource")]
    public string? TaskImageSource { get; set; }
}

public class LeaderboardEntryDto
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
}
