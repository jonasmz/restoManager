using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Application.Organization.Departments;

public sealed record DepartmentDto(int Id, int BranchId, string Name, string Description);

public sealed record SaveDepartmentCommand(int? Id, string Name, string Description);

public sealed class SaveDepartmentValidator : AbstractValidator<SaveDepartmentCommand>
{
    public SaveDepartmentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public sealed class SaveDepartmentHandler(
    IDepartmentRepository departments,
    IBranchContext branchContext,
    IUnitOfWork unitOfWork,
    IValidator<SaveDepartmentCommand> validator)
{
    public async Task<int> HandleAsync(SaveDepartmentCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        var branchId = branchContext.BranchId;

        Department entity;
        if (command.Id is { } id)
        {
            entity = await departments.GetAsync(id, ct) ?? throw new NotFoundException("departamento", id);
            if (entity.BranchId != branchId)
            {
                throw new BranchAccessDeniedException(entity.BranchId);
            }
            entity.Update(command.Name, command.Description);
        }
        else
        {
            entity = new Department(branchId, command.Name, command.Description);
            departments.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed class ListDepartmentsHandler(IDepartmentRepository departments, IBranchContext branchContext)
{
    public async Task<IReadOnlyList<DepartmentDto>> HandleAsync(CancellationToken ct = default)
    {
        var rows = await departments.ListByBranchAsync(branchContext.BranchId, ct);
        return rows.Select(d => new DepartmentDto(d.Id, d.BranchId, d.Name, d.Description)).ToList();
    }
}

public sealed class GetDepartmentHandler(IDepartmentRepository departments, IBranchContext branchContext)
{
    public async Task<DepartmentDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var d = await departments.GetAsync(id, ct) ?? throw new NotFoundException("departamento", id);
        if (d.BranchId != branchContext.BranchId)
        {
            throw new BranchAccessDeniedException(d.BranchId);
        }
        return new DepartmentDto(d.Id, d.BranchId, d.Name, d.Description);
    }
}
