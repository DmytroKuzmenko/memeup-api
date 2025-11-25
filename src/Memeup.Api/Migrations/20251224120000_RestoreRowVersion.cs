using System;
using Memeup.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memeup.Api.Migrations
{
    [DbContext(typeof(MemeupDbContext))]
    [Migration("20251224120000_RestoreRowVersion")]
    public partial class RestoreRowVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
