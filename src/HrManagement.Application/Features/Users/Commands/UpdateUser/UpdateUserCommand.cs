using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(Guid Id, bool IsActive, string Role, Guid? EmployeeId) : ICommand<UserDto>;
