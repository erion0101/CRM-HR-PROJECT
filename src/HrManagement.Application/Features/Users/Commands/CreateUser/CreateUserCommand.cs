using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand(string Email, string TemporaryPassword, string Role, Guid? EmployeeId) : ICommand<UserDto>;
