using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memeup.Api.Migrations
{
    public partial class RemoveRowVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Levels");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sections");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tasks",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Levels",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sections",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());
        }
    }
}
