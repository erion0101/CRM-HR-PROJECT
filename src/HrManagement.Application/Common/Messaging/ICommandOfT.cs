using MediatR;

namespace HrManagement.Application.Common.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse>;
