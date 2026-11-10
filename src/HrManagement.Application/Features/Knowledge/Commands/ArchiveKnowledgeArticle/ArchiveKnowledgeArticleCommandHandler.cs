using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Commands.ArchiveKnowledgeArticle;

public sealed class ArchiveKnowledgeArticleCommandHandler(IKnowledgeRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<ArchiveKnowledgeArticleCommand>
{
    public async Task Handle(ArchiveKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await repository.GetAsync(request.ArticleId, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
        if (!await repository.CanManageAsync(article, cancellationToken)) throw new NotFoundException("Knowledge article not found.");
        if (article.Version != request.ExpectedVersion) throw new ConflictException("The article was changed by another user.");
        article.ArchiveKnowledge(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
