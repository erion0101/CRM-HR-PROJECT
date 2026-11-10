using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizBackgroundGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenerationAttempts",
                table: "OnboardingQuizzes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GenerationError",
                table: "OnboardingQuizzes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GenerationLeaseUntilUtc",
                table: "OnboardingQuizzes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GenerationRequestedByUserId",
                table: "OnboardingQuizzes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GenerationStatus",
                table: "OnboardingQuizzes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizzes_GenerationStatus_GenerationLeaseUntilUtc",
                table: "OnboardingQuizzes",
                columns: new[] { "GenerationStatus", "GenerationLeaseUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OnboardingQuizzes_GenerationStatus_GenerationLeaseUntilUtc",
                table: "OnboardingQuizzes");

            migrationBuilder.DropColumn(
                name: "GenerationAttempts",
                table: "OnboardingQuizzes");

            migrationBuilder.DropColumn(
                name: "GenerationError",
                table: "OnboardingQuizzes");

            migrationBuilder.DropColumn(
                name: "GenerationLeaseUntilUtc",
                table: "OnboardingQuizzes");

            migrationBuilder.DropColumn(
                name: "GenerationRequestedByUserId",
                table: "OnboardingQuizzes");

            migrationBuilder.DropColumn(
                name: "GenerationStatus",
                table: "OnboardingQuizzes");
        }
    }
}
