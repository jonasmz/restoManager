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
