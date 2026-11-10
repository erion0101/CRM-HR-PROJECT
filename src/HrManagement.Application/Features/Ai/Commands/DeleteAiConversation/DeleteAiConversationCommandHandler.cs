using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Commands.DeleteAiConversation;

public sealed class DeleteAiConversationCommandHandler(IAiConversationRepository repository, IUnitOfWork unitOfWork) : ICommandHandler<DeleteAiConversationCommand>
{
    public async Task Handle(DeleteAiConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await repository.GetOwnedAsync(request.ConversationId, cancellationToken) ?? throw new NotFoundException("Conversation not found.");
        conversation.Archive(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
