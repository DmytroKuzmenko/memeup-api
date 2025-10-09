using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memeup.Api.Migrations
{
    public partial class GameProgress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActiveTaskAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    AttemptStartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveTaskAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskAttemptLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    IsTimeout = table.Column<bool>(type: "boolean", nullable: false),
                    TimeSpentSec = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    ShownExplanation = table.Column<bool>(type: "boolean", nullable: false),
                    ClientAgent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ClientTz = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IpHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAttemptLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLevelProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastRunScore = table.Column<int>(type: "integer", nullable: false),
                    BestScore = table.Column<int>(type: "integer", nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    RunsCount = table.Column<int>(type: "integer", nullable: false),
                    LastCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplayAvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLevelProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSectionProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LevelsCompleted = table.Column<int>(type: "integer", nullable: false),
                    TotalLevels = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSectionProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTaskProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptsUsed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PointsEarned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    TimeSpentSec = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTaskProgress", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveTaskAttempts_Token",
                table: "ActiveTaskAttempts",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActiveTaskAttempts_UserId_TaskId_IsFinalized",
                table: "ActiveTaskAttempts",
                columns: new[] { "UserId", "TaskId", "IsFinalized" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_UserId_Period",
                table: "LeaderboardEntries",
                columns: new[] { "UserId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskAttemptLogs_UserId_TaskId",
                table: "TaskAttemptLogs",
                columns: new[] { "UserId", "TaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLevelProgress_UserId_LevelId",
                table: "UserLevelProgress",
                columns: new[] { "UserId", "LevelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSectionProgress_UserId_SectionId",
                table: "UserSectionProgress",
                columns: new[] { "UserId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTaskProgress_UserId_TaskId",
                table: "UserTaskProgress",
                columns: new[] { "UserId", "TaskId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveTaskAttempts");

            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "TaskAttemptLogs");

            migrationBuilder.DropTable(
                name: "UserLevelProgress");

            migrationBuilder.DropTable(
                name: "UserSectionProgress");

            migrationBuilder.DropTable(
                name: "UserTaskProgress");
        }
    }
}
