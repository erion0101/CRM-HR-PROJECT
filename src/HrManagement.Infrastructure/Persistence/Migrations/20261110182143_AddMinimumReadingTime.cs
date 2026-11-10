using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMinimumReadingTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumReadingMinutes",
                table: "OnboardingTemplateSteps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumReadingMinutes",
                table: "OnboardingStepProgresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumReadingMinutes",
                table: "OnboardingReadingProgresses",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumReadingMinutes",
                table: "OnboardingTemplateSteps");

            migrationBuilder.DropColumn(
                name: "MinimumReadingMinutes",
                table: "OnboardingStepProgresses");

            migrationBuilder.DropColumn(
                name: "MinimumReadingMinutes",
                table: "OnboardingReadingProgresses");
        }
    }
}
