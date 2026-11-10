using MediatR;

namespace HrManagement.Application.Common.Messaging;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand> where TCommand : ICommand;
