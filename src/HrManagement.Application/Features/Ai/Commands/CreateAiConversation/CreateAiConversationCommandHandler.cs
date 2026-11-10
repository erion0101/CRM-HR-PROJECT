using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Ai.Commands.CreateAiConversation;

public sealed class CreateAiConversationCommandHandler(
    IAiConversationRepository repository,
    IAuthorizedDataScope scope,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    IAiRuntimeSettings settings) : ICommandHandler<CreateAiConversationCommand, AiConversationDto>
{
    public async Task<AiConversationDto> Handle(CreateAiConversationCommand request, CancellationToken cancellationToken)
    {
        if (request.ContextType == "Employee" && request.ContextId is { } employeeId && !await scope.CanAccessEmployeeAsync(employeeId, cancellationToken))
            throw new NotFoundException("Employee not found.");
        var userId = currentUser.UserId ?? throw new ForbiddenException();
        var now = DateTimeOffset.UtcNow;
        var conversation = AiConversation.Create(scope.OrganizationId, userId,
            string.IsNullOrWhiteSpace(request.Title) ? "New HR conversation" : request.Title.Trim(),
            request.ContextType, request.ContextId, await scope.GetAuthorizationFingerprintAsync(cancellationToken),
            now.AddDays(settings.Value.ConversationRetentionDays));
        repository.Add(conversation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AiConversationDto(conversation.Id, conversation.Title, conversation.ContextType, conversation.ContextId,
            conversation.CreatedAtUtc, conversation.UpdatedAtUtc, []);
    }
}
