using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Common.Models;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Onboarding.Queries.DownloadOnboardingDocument;

public sealed class DownloadOnboardingDocumentQueryHandler(IOnboardingRepository repository, ICurrentUserService currentUser,
    IDocumentStorageService storage, IAuditWriter audit) : IQueryHandler<DownloadOnboardingDocumentQuery, DocumentDownload>
{
    public async Task<DocumentDownload> Handle(DownloadOnboardingDocumentQuery request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetAssignmentAsync(request.AssignmentId, cancellationToken) ?? throw new NotFoundException("Onboarding journey not found.");
        if (currentUser.EmployeeId != assignment.EmployeeId) throw new NotFoundException("Onboarding journey not found.");
        if (assignment.CurrentReadingCycleId is not { } cycleId) throw new NotFoundException("Reading document not found.");
        var source = await repository.GetReadingDocumentAsync(cycleId, request.StepProgressId, cancellationToken) ?? throw new NotFoundException("Reading document not found.");
        var stream = await storage.OpenReadAsync(source.Version.StorageKey, cancellationToken);
        await audit.RecordAccessAsync("DownloadOnboardingReading", nameof(EmployeeDocument), source.Document.Id.ToString(), cancellationToken);
        return new DocumentDownload(source.Version.OriginalFileName, source.Version.ContentType, source.Version.SizeBytes, stream);
    }
}
