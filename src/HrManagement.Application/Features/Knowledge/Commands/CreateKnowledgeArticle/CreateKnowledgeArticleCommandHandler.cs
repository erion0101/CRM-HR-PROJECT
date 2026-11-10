using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Projects;
using HrManagement.Application.Common.Authorization;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Knowledge.Commands.CreateKnowledgeArticle;

public sealed class CreateKnowledgeArticleCommandHandler(
    IKnowledgeRepository repository,
    IProjectRepository projects,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateKnowledgeArticleCommand, KnowledgeArticleDto>
{
    public async Task<KnowledgeArticleDto> Handle(CreateKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var organizationId = currentUser.OrganizationId ?? throw new ForbiddenException();
        var userId = currentUser.UserId ?? throw new ForbiddenException();
        if (await repository.SlugExistsAsync(request.Slug, null, cancellationToken)) throw new ConflictException("A knowledge article with this slug already exists.");
        if (request.DepartmentId is { } departmentId && !await repository.DepartmentExistsAsync(departmentId, cancellationToken))
            throw new NotFoundException("Organization unit not found.");
        if (request.ProjectId is { } projectId)
        {
            if (!await projects.CanManageAsync(projectId, cancellationToken)) throw new NotFoundException("Project not found.");
        }
        else if (!currentUser.IsInRole(Roles.Hr))
        {
            throw new ForbiddenException();
        }
        var article = KnowledgeArticle.Create(organizationId, request.Slug, userId, request.Audience, request.ProjectId, request.DepartmentId, request.SortOrder);
        var revision = KnowledgeArticleRevision.Create(organizationId, article.Id, 1, request.Title, request.Description, request.Category,
            request.Content, userId, "Initial draft");
        repository.Add(article);
        repository.Add(revision);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetVisibleAsync(article.Id, cancellationToken) ?? throw new InvalidOperationException("Created article was not available.");
    }
}
