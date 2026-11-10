using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Employees.Commands.ArchiveEmployee;

public sealed record ArchiveEmployeeCommand(Guid Id, long ExpectedVersion) : ICommand;
