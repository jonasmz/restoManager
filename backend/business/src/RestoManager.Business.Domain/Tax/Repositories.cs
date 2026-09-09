namespace RestoManager.Business.Domain.Tax;

public interface ITaxRateRepository
{
    Task<TaxRate?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TaxRate>> ListAsync(string? search, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    void Add(TaxRate taxRate);
}
