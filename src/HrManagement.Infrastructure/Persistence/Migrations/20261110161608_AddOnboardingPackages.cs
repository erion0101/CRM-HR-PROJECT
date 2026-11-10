using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "OnboardingTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "OnboardingTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectTemplateId",
                table: "OnboardingAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectTemplateVersionId",
                table: "OnboardingAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectTemplateVersionNumber",
                table: "OnboardingAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplates_OrganizationId_Kind_ProjectId",
                table: "OnboardingTemplates",
                columns: new[] { "OrganizationId", "Kind", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplates_ProjectId",
                table: "OnboardingTemplates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_ProjectTemplateId",
                table: "OnboardingAssignments",
                column: "ProjectTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_ProjectTemplateVersionId",
                table: "OnboardingAssignments",
                column: "ProjectTemplateVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingAssignments_OnboardingTemplateVersions_ProjectTem~",
                table: "OnboardingAssignments",
                column: "ProjectTemplateVersionId",
                principalTable: "OnboardingTemplateVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingAssignments_OnboardingTemplates_ProjectTemplateId",
                table: "OnboardingAssignments",
                column: "ProjectTemplateId",
                principalTable: "OnboardingTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingTemplates_Projects_ProjectId",
                table: "OnboardingTemplates",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingAssignments_OnboardingTemplateVersions_ProjectTem~",
                table: "OnboardingAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingAssignments_OnboardingTemplates_ProjectTemplateId",
                table: "OnboardingAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingTemplates_Projects_ProjectId",
                table: "OnboardingTemplates");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingTemplates_OrganizationId_Kind_ProjectId",
                table: "OnboardingTemplates");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingTemplates_ProjectId",
                table: "OnboardingTemplates");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingAssignments_ProjectTemplateId",
                table: "OnboardingAssignments");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingAssignments_ProjectTemplateVersionId",
                table: "OnboardingAssignments");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "OnboardingTemplates");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "OnboardingTemplates");

            migrationBuilder.DropColumn(
                name: "ProjectTemplateId",
                table: "OnboardingAssignments");

            migrationBuilder.DropColumn(
                name: "ProjectTemplateVersionId",
                table: "OnboardingAssignments");

            migrationBuilder.DropColumn(
                name: "ProjectTemplateVersionNumber",
                table: "OnboardingAssignments");
        }
    }
}
