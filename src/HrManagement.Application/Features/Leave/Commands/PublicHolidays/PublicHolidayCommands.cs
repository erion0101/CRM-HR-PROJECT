using FluentValidation;
using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Messaging;
using HrManagement.Domain;

namespace HrManagement.Application.Features.Leave.Commands.PublicHolidays;

public sealed record CreatePublicHolidayCommand(DateOnly Date, string Name, bool RepeatsYearly) : ICommand<PublicHolidayDto>;
public sealed record DeletePublicHolidayCommand(Guid Id) : ICommand;

public sealed class CreatePublicHolidayCommandValidator : AbstractValidator<CreatePublicHolidayCommand>
{
    public CreatePublicHolidayCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Date.Year).InclusiveBetween(2000, 2100).WithName("Date");
    }
}

public sealed class CreatePublicHolidayCommandHandler(ILeaveRepository repository, IAuthorizedDataScope scope, IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePublicHolidayCommand, PublicHolidayDto>
{
    public async Task<PublicHolidayDto> Handle(CreatePublicHolidayCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.ListHolidaysAsync(cancellationToken);
        if (existing.Any(x => x.OccursIn(request.Date.Year) == request.Date)) throw new ConflictException("There is already a holiday on this day.");
        var holiday = PublicHoliday.Create(scope.OrganizationId, request.Date, request.Name, request.RepeatsYearly);
        repository.AddHoliday(holiday);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return holiday.ToDto();
    }
}

/// <summary>Removing a holiday does not change requests already made; their working days were fixed when they were sent.</summary>
public sealed class DeletePublicHolidayCommandHandler(ILeaveRepository repository, IUnitOfWork unitOfWork) : ICommandHandler<DeletePublicHolidayCommand>
{
    public async Task Handle(DeletePublicHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = await repository.GetHolidayAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Holiday not found.");
        repository.RemoveHoliday(holiday);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
