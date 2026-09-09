using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Application.Organization.Roles;

public sealed record RoleDto(int Id, string Name, string Description, decimal HourlyRate);

public sealed record SaveRoleCommand(int? Id, string Name, string Description, decimal HourlyRate);

public sealed class SaveRoleValidator : AbstractValidator<SaveRoleCommand>
{
    public SaveRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveRoleHandler(
    IRoleRepository roles, IUnitOfWork unitOfWork, IValidator<SaveRoleCommand> validator)
{
    public async Task<int> HandleAsync(SaveRoleCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        Role entity;
        if (command.Id is { } id)
        {
            entity = await roles.GetAsync(id, ct) ?? throw new NotFoundException("rol", id);
            entity.Update(command.Name, command.Description, command.HourlyRate);
        }
        else
        {
            entity = new Role(command.Name, command.Description, command.HourlyRate);
            roles.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed record ListRolesQuery(int Page = 1, int PageSize = 20);

public sealed class ListRolesHandler(IRoleRepository roles)
{
    public async Task<PagedResult<RoleDto>> HandleAsync(ListRolesQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await roles.ListAsync(page.Skip, page.Take, ct);
        var total = await roles.CountAsync(ct);
        return new PagedResult<RoleDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static RoleDto Map(Role r) => new(r.Id, r.Name, r.Description, r.HourlyRate);
}

public sealed class GetRoleHandler(IRoleRepository roles)
{
    public async Task<RoleDto> HandleAsync(int id, CancellationToken ct = default)
        => ListRolesHandler.Map(await roles.GetAsync(id, ct) ?? throw new NotFoundException("rol", id));
}
