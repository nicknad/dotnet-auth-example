using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Api.Migrations;

/// <inheritdoc />
internal partial class AddPreviousRefreshToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PreviousRefreshToken",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PreviousRefreshTokenExpiresAt",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PreviousRefreshToken",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PreviousRefreshTokenExpiresAt",
            table: "AspNetUsers");
    }
}
