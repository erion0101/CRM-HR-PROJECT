using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(string Email, string Password, bool RememberMe = false) : ICommand<bool>;
