using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Application.Features.Employees;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Compensation.Commands.AddCompensation;

public sealed class AddCompensationCommandHandler(ICompensationRepository compensation, IEmployeeRepository employees, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : ICommandHandler<AddCompensationCommand, CompensationDto>
{
    public async Task<CompensationDto> Handle(AddCompensationCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Hr)) throw new ForbiddenException();
        if (!await employees.ExistsAccessibleAsync(request.EmployeeId, cancellationToken)) throw new NotFoundException("Employee not found.");
        if (await compensation.HasOverlapAsync(request.EmployeeId, request.EffectiveFrom, request.EffectiveTo, cancellationToken)) throw new ConflictException("Compensation dates overlap an existing record.");
        var entity = CompensationRecord.Create(currentUser.OrganizationId ?? throw new ForbiddenException(), request.EmployeeId, request.Amount, request.Currency, request.Frequency, request.EffectiveFrom, request.EffectiveTo);
        compensation.Add(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }
}
