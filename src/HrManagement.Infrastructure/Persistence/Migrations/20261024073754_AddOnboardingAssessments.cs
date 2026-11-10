using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssessmentPassedAtUtc",
                table: "OnboardingAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssessmentStatus",
                table: "OnboardingAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentReadingCycleId",
                table: "OnboardingAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentReadingCycleNumber",
                table: "OnboardingAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OnboardingQuizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizVersion = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GenerationSource = table.Column<int>(type: "integer", nullable: false),
                    PassingPercentage = table.Column<int>(type: "integer", nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    RandomizeQuestions = table.Column<bool>(type: "boolean", nullable: false),
                    RandomizeOptions = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizzes_OnboardingTemplateVersions_TemplateVersio~",
                        column: x => x.TemplateVersionId,
                        principalTable: "OnboardingTemplateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingQuizQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Topic = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceDocumentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceKnowledgeRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceLabel = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    SourcePage = table.Column<int>(type: "integer", nullable: true),
                    SourceParagraph = table.Column<int>(type: "integer", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuizQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizQuestions_DocumentVersions_SourceDocumentVers~",
                        column: x => x.SourceDocumentVersionId,
                        principalTable: "DocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizQuestions_Documents_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizQuestions_KnowledgeArticleRevisions_SourceKno~",
                        column: x => x.SourceKnowledgeRevisionId,
                        principalTable: "KnowledgeArticleRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizQuestions_OnboardingQuizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "OnboardingQuizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingQuizAnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuizAnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizAnswerOptions_OnboardingQuizQuestions_Questio~",
                        column: x => x.QuestionId,
                        principalTable: "OnboardingQuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingQuizAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuizAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizAnswers_OnboardingQuizAnswerOptions_SelectedO~",
                        column: x => x.SelectedOptionId,
                        principalTable: "OnboardingQuizAnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizAnswers_OnboardingQuizQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "OnboardingQuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingQuizAttemptQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OptionOrder = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuizAttemptQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizAttemptQuestions_OnboardingQuizQuestions_Ques~",
                        column: x => x.QuestionId,
                        principalTable: "OnboardingQuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingQuizAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadingCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PassingPercentage = table.Column<int>(type: "integer", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "integer", nullable: true),
                    TotalQuestions = table.Column<int>(type: "integer", nullable: true),
                    ScorePercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuizAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizAttempts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizAttempts_OnboardingAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "OnboardingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingQuizAttempts_OnboardingQuizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "OnboardingQuizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingReadingCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingReadingCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingReadingCycles_OnboardingAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "OnboardingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnboardingReadingCycles_OnboardingQuizAttempts_FailedAttemp~",
                        column: x => x.FailedAttemptId,
                        principalTable: "OnboardingQuizAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingReadingProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadingCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepProgressId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    KnowledgeArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    KnowledgeRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingReadingProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingReadingProgresses_DocumentVersions_DocumentVersio~",
                        column: x => x.DocumentVersionId,
                        principalTable: "DocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingReadingProgresses_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingReadingProgresses_KnowledgeArticleRevisions_Knowl~",
                        column: x => x.KnowledgeRevisionId,
                        principalTable: "KnowledgeArticleRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingReadingProgresses_KnowledgeArticles_KnowledgeArti~",
                        column: x => x.KnowledgeArticleId,
                        principalTable: "KnowledgeArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingReadingProgresses_OnboardingReadingCycles_Reading~",
                        column: x => x.ReadingCycleId,
                        principalTable: "OnboardingReadingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnboardingReadingProgresses_OnboardingStepProgresses_StepPr~",
                        column: x => x.StepProgressId,
                        principalTable: "OnboardingStepProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAnswerOptions_OrganizationId",
                table: "OnboardingQuizAnswerOptions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAnswerOptions_QuestionId_SortOrder",
                table: "OnboardingQuizAnswerOptions",
                columns: new[] { "QuestionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAnswers_AttemptId_QuestionId",
                table: "OnboardingQuizAnswers",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAnswers_OrganizationId",
                table: "OnboardingQuizAnswers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAnswers_QuestionId",
                table: "OnboardingQuizAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAnswers_SelectedOptionId",
                table: "OnboardingQuizAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttemptQuestions_AttemptId_QuestionId",
                table: "OnboardingQuizAttemptQuestions",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttemptQuestions_AttemptId_SortOrder",
                table: "OnboardingQuizAttemptQuestions",
                columns: new[] { "AttemptId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttemptQuestions_OrganizationId",
                table: "OnboardingQuizAttemptQuestions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttemptQuestions_QuestionId",
                table: "OnboardingQuizAttemptQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttempts_AssignmentId_AttemptNumber",
                table: "OnboardingQuizAttempts",
                columns: new[] { "AssignmentId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttempts_EmployeeId",
                table: "OnboardingQuizAttempts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttempts_OrganizationId",
                table: "OnboardingQuizAttempts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttempts_OrganizationId_EmployeeId_Status",
                table: "OnboardingQuizAttempts",
                columns: new[] { "OrganizationId", "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttempts_QuizId",
                table: "OnboardingQuizAttempts",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizAttempts_ReadingCycleId",
                table: "OnboardingQuizAttempts",
                column: "ReadingCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizQuestions_OrganizationId",
                table: "OnboardingQuizQuestions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizQuestions_QuizId_SortOrder",
                table: "OnboardingQuizQuestions",
                columns: new[] { "QuizId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizQuestions_SourceDocumentId",
                table: "OnboardingQuizQuestions",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizQuestions_SourceDocumentVersionId",
                table: "OnboardingQuizQuestions",
                column: "SourceDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizQuestions_SourceKnowledgeRevisionId",
                table: "OnboardingQuizQuestions",
                column: "SourceKnowledgeRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizzes_OrganizationId",
                table: "OnboardingQuizzes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizzes_OrganizationId_TemplateVersionId_Status",
                table: "OnboardingQuizzes",
                columns: new[] { "OrganizationId", "TemplateVersionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuizzes_TemplateVersionId_QuizVersion",
                table: "OnboardingQuizzes",
                columns: new[] { "TemplateVersionId", "QuizVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingCycles_AssignmentId_CycleNumber",
                table: "OnboardingReadingCycles",
                columns: new[] { "AssignmentId", "CycleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingCycles_FailedAttemptId",
                table: "OnboardingReadingCycles",
                column: "FailedAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingCycles_OrganizationId",
                table: "OnboardingReadingCycles",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingCycles_OrganizationId_AssignmentId_Status",
                table: "OnboardingReadingCycles",
                columns: new[] { "OrganizationId", "AssignmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingProgresses_DocumentId",
                table: "OnboardingReadingProgresses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingProgresses_DocumentVersionId",
                table: "OnboardingReadingProgresses",
                column: "DocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingProgresses_KnowledgeArticleId",
                table: "OnboardingReadingProgresses",
                column: "KnowledgeArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingProgresses_KnowledgeRevisionId",
                table: "OnboardingReadingProgresses",
                column: "KnowledgeRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingProgresses_OrganizationId",
                table: "OnboardingReadingProgresses",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingProgresses_OrganizationId_ReadingCycleId_S~",
                table: "OnboardingReadingProgresses",
                columns: new[] { "OrganizationId", "ReadingCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingProgresses_ReadingCycleId_StepProgressId",
                table: "OnboardingReadingProgresses",
                columns: new[] { "ReadingCycleId", "StepProgressId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingReadingProgresses_StepProgressId",
                table: "OnboardingReadingProgresses",
                column: "StepProgressId");

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingQuizAnswers_OnboardingQuizAttempts_AttemptId",
                table: "OnboardingQuizAnswers",
                column: "AttemptId",
                principalTable: "OnboardingQuizAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingQuizAttemptQuestions_OnboardingQuizAttempts_Attem~",
                table: "OnboardingQuizAttemptQuestions",
                column: "AttemptId",
                principalTable: "OnboardingQuizAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingQuizAttempts_OnboardingReadingCycles_ReadingCycle~",
                table: "OnboardingQuizAttempts",
                column: "ReadingCycleId",
                principalTable: "OnboardingReadingCycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingReadingCycles_OnboardingQuizAttempts_FailedAttemp~",
                table: "OnboardingReadingCycles");

            migrationBuilder.DropTable(
                name: "OnboardingQuizAnswers");

            migrationBuilder.DropTable(
                name: "OnboardingQuizAttemptQuestions");

            migrationBuilder.DropTable(
                name: "OnboardingReadingProgresses");

            migrationBuilder.DropTable(
                name: "OnboardingQuizAnswerOptions");

            migrationBuilder.DropTable(
                name: "OnboardingQuizQuestions");

            migrationBuilder.DropTable(
                name: "OnboardingQuizAttempts");

            migrationBuilder.DropTable(
                name: "OnboardingQuizzes");

            migrationBuilder.DropTable(
                name: "OnboardingReadingCycles");

            migrationBuilder.DropColumn(
                name: "AssessmentPassedAtUtc",
                table: "OnboardingAssignments");

            migrationBuilder.DropColumn(
                name: "AssessmentStatus",
                table: "OnboardingAssignments");

            migrationBuilder.DropColumn(
                name: "CurrentReadingCycleId",
                table: "OnboardingAssignments");

            migrationBuilder.DropColumn(
                name: "CurrentReadingCycleNumber",
                table: "OnboardingAssignments");
        }
    }
}
