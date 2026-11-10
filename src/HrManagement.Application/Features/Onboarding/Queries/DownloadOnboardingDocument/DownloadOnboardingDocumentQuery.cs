using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;

namespace HrManagement.Application.Features.Onboarding.Queries.DownloadOnboardingDocument;

public sealed record DownloadOnboardingDocumentQuery(Guid AssignmentId, Guid StepProgressId) : IQuery<DocumentDownload>;
