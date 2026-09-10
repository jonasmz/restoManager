using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Customers;

namespace RestoManager.Business.Application.Customers;

// Reseñas de clientes por sucursal (Fase 8). Alta y listado, scope por X-Branch-Id.

public sealed record ReviewDto(
    int Id, int CustomerId, int BranchId, int Rating, string? Comment, DateOnly ReviewDate);

// ---- Alta ----

public sealed record CreateReviewCommand(int CustomerId, int Rating, string? Comment);

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}

public sealed class CreateReviewHandler(
    IReviewRepository reviews,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IBranchContext branchContext,
    BranchAccessGuard access,
    IClock clock,
    IValidator<CreateReviewCommand> validator)
{
    public async Task<int> HandleAsync(CreateReviewCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var branchId = branchContext.BranchId;
        access.EnsureCanOperate(branchId);

        if (!await customers.ExistsAsync(command.CustomerId, ct))
        {
            throw new NotFoundException("cliente", command.CustomerId);
        }

        var review = Review.Create(
            command.CustomerId, branchId, command.Rating, command.Comment,
            DateOnly.FromDateTime(clock.UtcNow));
        reviews.Add(review);
        await unitOfWork.SaveChangesAsync(ct);
        return review.Id;
    }
}

// ---- Listado (sucursal activa) ----

public sealed record ListReviewsQuery(int? MinRating, int Page = 1, int PageSize = 20);

public sealed class ListReviewsHandler(IReviewRepository reviews, IBranchContext branchContext)
{
    public async Task<PagedResult<ReviewDto>> HandleAsync(ListReviewsQuery query, CancellationToken ct = default)
    {
        var branchId = branchContext.BranchId;
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await reviews.ListForBranchAsync(branchId, query.MinRating, page.Skip, page.Take, ct);
        var total = await reviews.CountForBranchAsync(branchId, query.MinRating, ct);
        return new PagedResult<ReviewDto>(
            items.Select(r => new ReviewDto(r.Id, r.CustomerId, r.BranchId, r.Rating, r.Comment, r.ReviewDate)).ToList(),
            query.Page, page.Take, total);
    }
}
