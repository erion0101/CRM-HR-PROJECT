using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Commands.AddKnowledgeRevision;

public sealed class AddKnowledgeRevisionCommandHandler(IKnowledgeRepository repository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<AddKnowledgeRevisionCommand, KnowledgeArticleDto>
{
    public async Task<KnowledgeArticleDto> Handle(AddKnowledgeRevisionCommand request, CancellationToken cancellationToken)
    {
        var article = await repository.GetAsync(request.ArticleId, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
        if (!await repository.CanManageAsync(article, cancellationToken)) throw new NotFoundException("Knowledge article not found.");
        if (article.Version != request.ExpectedVersion) throw new ConflictException("The article was changed by another user.");
        if (request.DepartmentId is { } departmentId && !await repository.DepartmentExistsAsync(departmentId, cancellationToken))
            throw new NotFoundException("Organization unit not found.");
        var userId = currentUser.UserId ?? throw new ForbiddenException();
        var revisionNumber = article.AddRevision(userId, request.Audience, request.DepartmentId, request.SortOrder, DateTimeOffset.UtcNow);
        repository.Add(KnowledgeArticleRevision.Create(article.OrganizationId, article.Id, revisionNumber, request.Title, request.Description,
            request.Category, request.Content, userId, request.ChangeSummary));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetVisibleAsync(article.Id, cancellationToken) ?? throw new InvalidOperationException("Updated article was not available.");
    }
}
