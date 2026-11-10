using MediatR;

namespace HrManagement.Application.Common.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
