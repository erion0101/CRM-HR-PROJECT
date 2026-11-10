using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Ai.Commands.TestAiProvider;

public sealed class TestAiProviderCommandHandler(IAiProvider provider) : ICommandHandler<TestAiProviderCommand, bool>
{
    public Task<bool> Handle(TestAiProviderCommand request, CancellationToken cancellationToken) => provider.TestConnectionAsync(cancellationToken);
}
