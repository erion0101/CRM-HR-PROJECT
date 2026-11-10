using HrManagement.Application.Common.Messaging;

namespace HrManagement.Application.Features.Departments.Commands.ArchiveDepartment;

public sealed record ArchiveDepartmentCommand(Guid Id, long ExpectedVersion) : ICommand;
