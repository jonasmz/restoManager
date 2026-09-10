using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Customers;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(BusinessDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetAsync(int id, CancellationToken ct = default)
        => db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => db.Customers.AnyAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Customer>> ListAsync(
        string? search, int skip, int take, CancellationToken ct = default)
        => await Filter(search).OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(string? search, CancellationToken ct = default)
        => Filter(search).CountAsync(ct);

    public void Add(Customer customer) => db.Customers.Add(customer);

    private IQueryable<Customer> Filter(string? search)
    {
        var q = db.Customers.AsQueryable();
        if (string.IsNullOrWhiteSpace(search))
        {
            return q;
        }
        var term = $"%{search.Trim()}%";
        return q.Where(x =>
            EF.Functions.ILike(x.FirstName, term)
            || EF.Functions.ILike(x.LastName, term)
            || EF.Functions.ILike(x.Phone, term));
    }
}

public sealed class GiftCardRepository(BusinessDbContext db) : IGiftCardRepository
{
    public Task<GiftCard?> GetAsync(int id, CancellationToken ct = default)
        => db.GiftCards.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<GiftCard?> GetByCardNumberAsync(string cardNumber, CancellationToken ct = default)
        => db.GiftCards.FirstOrDefaultAsync(x => x.CardNumber == cardNumber, ct);

    public Task<bool> ExistsByCardNumberAsync(string cardNumber, CancellationToken ct = default)
        => db.GiftCards.AnyAsync(x => x.CardNumber == cardNumber, ct);

    public async Task<IReadOnlyList<GiftCard>> ListForCustomerAsync(int customerId, CancellationToken ct = default)
        => await db.GiftCards.Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.Id).ToListAsync(ct);

    public void Add(GiftCard giftCard) => db.GiftCards.Add(giftCard);

    public void AddTransaction(GiftCardTransaction transaction) => db.GiftCardTransactions.Add(transaction);
}

public sealed class ReviewRepository(BusinessDbContext db) : IReviewRepository
{
    public async Task<IReadOnlyList<Review>> ListForBranchAsync(
        int branchId, int? minRating, int skip, int take, CancellationToken ct = default)
        => await Filter(branchId, minRating)
            .OrderByDescending(x => x.ReviewDate).ThenByDescending(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountForBranchAsync(int branchId, int? minRating, CancellationToken ct = default)
        => Filter(branchId, minRating).CountAsync(ct);

    public void Add(Review review) => db.Reviews.Add(review);

    private IQueryable<Review> Filter(int branchId, int? minRating)
    {
        var q = db.Reviews.Where(x => x.BranchId == branchId);
        return minRating is { } min ? q.Where(x => x.Rating >= min) : q;
    }
}
