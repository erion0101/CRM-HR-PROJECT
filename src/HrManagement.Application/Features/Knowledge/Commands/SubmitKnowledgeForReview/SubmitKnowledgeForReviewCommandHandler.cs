using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Knowledge.Commands.SubmitKnowledgeForReview;

public sealed class SubmitKnowledgeForReviewCommandHandler(IKnowledgeRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<SubmitKnowledgeForReviewCommand, KnowledgeArticleDto>
{
    public async Task<KnowledgeArticleDto> Handle(SubmitKnowledgeForReviewCommand request, CancellationToken cancellationToken)
    {
        var article = await repository.GetAsync(request.ArticleId, cancellationToken) ?? throw new NotFoundException("Knowledge article not found.");
        if (!await repository.CanManageAsync(article, cancellationToken)) throw new NotFoundException("Knowledge article not found.");
        if (article.Version != request.ExpectedVersion) throw new ConflictException("The article was changed by another user.");
        article.SubmitForReview(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await repository.GetVisibleAsync(article.Id, cancellationToken) ?? throw new InvalidOperationException("Article was not available.");
    }
}
