using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Delivery;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Application.Deliveries;

public sealed record DriverDto(int Id, int EmployeeId, string VehicleType, string LicensePlate);

// ---- Guardar (crea o actualiza) ----
public sealed record SaveDriverCommand(int? Id, int EmployeeId, string VehicleType, string LicensePlate);

public sealed class SaveDriverValidator : AbstractValidator<SaveDriverCommand>
{
    public SaveDriverValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.VehicleType)
            .Must(v => VehicleTypeExtensions.TryFromDbValue(v, out _))
            .WithMessage("Vehículo inválido. Use MOTORCYCLE, BICYCLE, CAR u ON_FOOT.");
        RuleFor(x => x.LicensePlate).NotEmpty().MaximumLength(20);
    }
}

public sealed class SaveDriverHandler(
    IDeliveryDriverRepository drivers,
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork,
    IValidator<SaveDriverCommand> validator)
{
    public async Task<int> HandleAsync(SaveDriverCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        var vehicle = VehicleTypeExtensions.FromDbValue(command.VehicleType);

        if (await employees.GetAsync(command.EmployeeId, ct) is null)
        {
            throw new NotFoundException("empleado", command.EmployeeId);
        }

        DeliveryDriver entity;
        if (command.Id is { } id)
        {
            entity = await drivers.GetAsync(id, ct) ?? throw new NotFoundException("repartidor", id);
            entity.Update(vehicle, command.LicensePlate);
        }
        else
        {
            entity = new DeliveryDriver(command.EmployeeId, vehicle, command.LicensePlate);
            drivers.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

// ---- Consultas ----
public sealed record ListDriversQuery(string? Search, int Page = 1, int PageSize = 20);

public sealed class ListDriversHandler(IDeliveryDriverRepository drivers)
{
    public async Task<PagedResult<DriverDto>> HandleAsync(ListDriversQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await drivers.ListAsync(query.Search, page.Skip, page.Take, ct);
        var total = await drivers.CountAsync(query.Search, ct);
        return new PagedResult<DriverDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static DriverDto Map(DeliveryDriver d) =>
        new(d.Id, d.EmployeeId, d.VehicleType.ToDbValue(), d.LicensePlate);
}

public sealed class GetDriverHandler(IDeliveryDriverRepository drivers)
{
    public async Task<DriverDto> HandleAsync(int id, CancellationToken ct = default)
        => ListDriversHandler.Map(await drivers.GetAsync(id, ct) ?? throw new NotFoundException("repartidor", id));
}
