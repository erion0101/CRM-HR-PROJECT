using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingAndKnowledgeManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnboardingTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                    PublishedVersion = table.Column<int>(type: "integer", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTemplates_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingTemplates_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnboardingContactEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Employees_LeadEmployeeId",
                        column: x => x.LeadEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Employees_OnboardingContactEmployeeId",
                        column: x => x.OnboardingContactEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingTemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTemplateVersions_OnboardingTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "OnboardingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Audience = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LatestRevision = table.Column<int>(type: "integer", nullable: false),
                    PublishedRevision = table.Column<int>(type: "integer", nullable: true),
                    LastReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticles_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectRole = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Responsibilities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMemberships_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMemberships_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectResourceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectResourceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectResourceLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManagerEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    MentorEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAssignments_Employees_ManagerEmployeeId",
                        column: x => x.ManagerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAssignments_Employees_MentorEmployeeId",
                        column: x => x.MentorEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAssignments_OnboardingTemplateVersions_TemplateVe~",
                        column: x => x.TemplateVersionId,
                        principalTable: "OnboardingTemplateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAssignments_OnboardingTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "OnboardingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAssignments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeArticleRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false),
                    ContentFormat = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeArticleRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticleRevisions_KnowledgeArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "KnowledgeArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTemplateSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionTitle = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SectionOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ExpectedOutcome = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StepType = table.Column<int>(type: "integer", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    DueAfterDays = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    KnowledgeArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DependsOnStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingTemplateSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTemplateSteps_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingTemplateSteps_KnowledgeArticles_KnowledgeArticleId",
                        column: x => x.KnowledgeArticleId,
                        principalTable: "KnowledgeArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingTemplateSteps_OnboardingTemplateSteps_DependsOnSt~",
                        column: x => x.DependsOnStepId,
                        principalTable: "OnboardingTemplateSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingTemplateSteps_OnboardingTemplateVersions_Template~",
                        column: x => x.TemplateVersionId,
                        principalTable: "OnboardingTemplateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingStepProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionTitle = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SectionOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ExpectedOutcome = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StepType = table.Column<int>(type: "integer", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    KnowledgeArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    KnowledgeRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DependsOnTemplateStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletionNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingStepProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingStepProgresses_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingStepProgresses_KnowledgeArticleRevisions_Knowledg~",
                        column: x => x.KnowledgeRevisionId,
                        principalTable: "KnowledgeArticleRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingStepProgresses_KnowledgeArticles_KnowledgeArticle~",
                        column: x => x.KnowledgeArticleId,
                        principalTable: "KnowledgeArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingStepProgresses_OnboardingAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "OnboardingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnboardingStepProgresses_OnboardingTemplateSteps_TemplateSt~",
                        column: x => x.TemplateStepId,
                        principalTable: "OnboardingTemplateSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingAcknowledgments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepProgressId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingAcknowledgments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingAcknowledgments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAcknowledgments_KnowledgeArticleRevisions_Knowled~",
                        column: x => x.KnowledgeRevisionId,
                        principalTable: "KnowledgeArticleRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAcknowledgments_KnowledgeArticles_KnowledgeArticl~",
                        column: x => x.KnowledgeArticleId,
                        principalTable: "KnowledgeArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAcknowledgments_OnboardingAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "OnboardingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingAcknowledgments_OnboardingStepProgresses_StepProg~",
                        column: x => x.StepProgressId,
                        principalTable: "OnboardingStepProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticleRevisions_ArticleId_RevisionNumber",
                table: "KnowledgeArticleRevisions",
                columns: new[] { "ArticleId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticleRevisions_OrganizationId",
                table: "KnowledgeArticleRevisions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticleRevisions_OrganizationId_Category",
                table: "KnowledgeArticleRevisions",
                columns: new[] { "OrganizationId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_DepartmentId",
                table: "KnowledgeArticles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_OrganizationId",
                table: "KnowledgeArticles",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_OrganizationId_ProjectId_Status_SortOrder",
                table: "KnowledgeArticles",
                columns: new[] { "OrganizationId", "ProjectId", "Status", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_OrganizationId_Slug",
                table: "KnowledgeArticles",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_ProjectId",
                table: "KnowledgeArticles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAcknowledgments_AssignmentId",
                table: "OnboardingAcknowledgments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAcknowledgments_EmployeeId",
                table: "OnboardingAcknowledgments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAcknowledgments_KnowledgeArticleId",
                table: "OnboardingAcknowledgments",
                column: "KnowledgeArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAcknowledgments_KnowledgeRevisionId",
                table: "OnboardingAcknowledgments",
                column: "KnowledgeRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAcknowledgments_OrganizationId",
                table: "OnboardingAcknowledgments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAcknowledgments_StepProgressId_KnowledgeRevisionId",
                table: "OnboardingAcknowledgments",
                columns: new[] { "StepProgressId", "KnowledgeRevisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_EmployeeId_TemplateId_TemplateVersion~",
                table: "OnboardingAssignments",
                columns: new[] { "EmployeeId", "TemplateId", "TemplateVersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_ManagerEmployeeId",
                table: "OnboardingAssignments",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_MentorEmployeeId",
                table: "OnboardingAssignments",
                column: "MentorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_OrganizationId",
                table: "OnboardingAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_OrganizationId_EmployeeId_Status",
                table: "OnboardingAssignments",
                columns: new[] { "OrganizationId", "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_ProjectId",
                table: "OnboardingAssignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_TemplateId",
                table: "OnboardingAssignments",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingAssignments_TemplateVersionId",
                table: "OnboardingAssignments",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepProgresses_AssignmentId_TemplateStepId",
                table: "OnboardingStepProgresses",
                columns: new[] { "AssignmentId", "TemplateStepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepProgresses_DocumentId",
                table: "OnboardingStepProgresses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepProgresses_KnowledgeArticleId",
                table: "OnboardingStepProgresses",
                column: "KnowledgeArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepProgresses_KnowledgeRevisionId",
                table: "OnboardingStepProgresses",
                column: "KnowledgeRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepProgresses_OrganizationId",
                table: "OnboardingStepProgresses",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepProgresses_OrganizationId_DueDate_Status",
                table: "OnboardingStepProgresses",
                columns: new[] { "OrganizationId", "DueDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepProgresses_TemplateStepId",
                table: "OnboardingStepProgresses",
                column: "TemplateStepId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplates_DepartmentId",
                table: "OnboardingTemplates",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplates_OrganizationId",
                table: "OnboardingTemplates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplates_OrganizationId_Status_Name",
                table: "OnboardingTemplates",
                columns: new[] { "OrganizationId", "Status", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplates_PositionId",
                table: "OnboardingTemplates",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplateSteps_DependsOnStepId",
                table: "OnboardingTemplateSteps",
                column: "DependsOnStepId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplateSteps_DocumentId",
                table: "OnboardingTemplateSteps",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplateSteps_KnowledgeArticleId",
                table: "OnboardingTemplateSteps",
                column: "KnowledgeArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplateSteps_OrganizationId",
                table: "OnboardingTemplateSteps",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplateSteps_TemplateVersionId_SectionOrder_Step~",
                table: "OnboardingTemplateSteps",
                columns: new[] { "TemplateVersionId", "SectionOrder", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplateVersions_OrganizationId",
                table: "OnboardingTemplateVersions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTemplateVersions_TemplateId_VersionNumber",
                table: "OnboardingTemplateVersions",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberships_EmployeeId",
                table: "ProjectMemberships",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberships_OrganizationId",
                table: "ProjectMemberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberships_OrganizationId_EmployeeId_EffectiveFrom",
                table: "ProjectMemberships",
                columns: new[] { "OrganizationId", "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberships_ProjectId_EmployeeId_EffectiveFrom",
                table: "ProjectMemberships",
                columns: new[] { "ProjectId", "EmployeeId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectResourceLinks_OrganizationId",
                table: "ProjectResourceLinks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectResourceLinks_ProjectId_SortOrder",
                table: "ProjectResourceLinks",
                columns: new[] { "ProjectId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DepartmentId",
                table: "Projects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_LeadEmployeeId",
                table: "Projects",
                column: "LeadEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OnboardingContactEmployeeId",
                table: "Projects",
                column: "OnboardingContactEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId",
                table: "Projects",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId_Code",
                table: "Projects",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId_Status_Name",
                table: "Projects",
                columns: new[] { "OrganizationId", "Status", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnboardingAcknowledgments");

            migrationBuilder.DropTable(
                name: "ProjectMemberships");

            migrationBuilder.DropTable(
                name: "ProjectResourceLinks");

            migrationBuilder.DropTable(
                name: "OnboardingStepProgresses");

            migrationBuilder.DropTable(
                name: "KnowledgeArticleRevisions");

            migrationBuilder.DropTable(
                name: "OnboardingAssignments");

            migrationBuilder.DropTable(
                name: "OnboardingTemplateSteps");

            migrationBuilder.DropTable(
                name: "KnowledgeArticles");

            migrationBuilder.DropTable(
                name: "OnboardingTemplateVersions");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "OnboardingTemplates");
        }
    }
}
