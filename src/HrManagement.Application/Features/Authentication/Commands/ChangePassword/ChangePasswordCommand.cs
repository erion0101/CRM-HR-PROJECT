using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Authentication.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;
