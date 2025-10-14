using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memeup.Api.Migrations
{
    /// <inheritdoc />
    public partial class TaskCharsAndAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CharsCsv",
                table: "Tasks",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswer",
                table: "Tasks",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CharsCsv",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CorrectAnswer",
                table: "Tasks");
        }
    }
}
