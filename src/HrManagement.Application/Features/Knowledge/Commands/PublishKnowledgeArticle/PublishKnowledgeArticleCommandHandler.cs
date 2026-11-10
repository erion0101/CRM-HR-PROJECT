using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Commands.PublishKnowledgeArticle;

public sealed class PublishKnowledgeArticleCommandHandler(IKnowledgeRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<PublishKnowledgeArticleCommand, KnowledgeArticleDto>
{
    public async Task<KnowledgeArticleDto> Handle(PublishKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await repository.GetAsync(request.ArticleId, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
        if (!await repository.CanManageAsync(article, cancellationToken)) throw new NotFoundException("Knowledge article not found.");
        if (article.Version != request.ExpectedVersion) throw new ConflictException("The article was changed by another user.");
        article.Publish(article.LatestRevision, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetVisibleAsync(article.Id, cancellationToken) ?? throw new InvalidOperationException("Published article was not available.");
    }
}
