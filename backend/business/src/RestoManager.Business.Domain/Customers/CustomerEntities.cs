using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Customers;

// Módulo Cliente. La Fase 5 añade un alta rápida mínima (nombre + contacto) para
// poder crear reservas antes de la Fase 8. La Fase 8 añade fidelización, gift cards
// y reseñas. gift_card_transactions se materializa en la Fase 6 (pago).

public sealed class Customer
{
    public int Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public int LoyaltyPoints { get; private set; }

    private Customer() { }

    public Customer(string firstName, string lastName, string phone, string email)
    {
        LoyaltyPoints = 0;
        Update(firstName, lastName, phone, email);
    }

    public void Update(string firstName, string lastName, string phone, string email)
    {
        FirstName = Required(firstName, "nombre");
        LastName = Required(lastName, "apellido");
        Phone = Required(phone, "teléfono");
        Email = email?.Trim() ?? string.Empty;
    }

    private static string Required(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainRuleException("customers.missing_field", $"El {field} del cliente es obligatorio.")
            : value.Trim();
}

public sealed class Review
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int BranchId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateOnly ReviewDate { get; set; }
}

public sealed class GiftCard
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public decimal Balance { get; private set; }
    public DateOnly ExpiryDate { get; set; }

    /// <summary>
    /// Canje de la tarjeta como medio de pago de un pedido (Fase 6b). Descuenta
    /// <paramref name="amount"/> del saldo y devuelve el asiento (<c>amount</c>
    /// negativo). La emisión y recarga del saldo son de la Fase 8.
    /// </summary>
    public GiftCardTransaction Redeem(int orderId, decimal amount, DateTime now)
    {
        if (amount <= 0m)
        {
            throw new DomainRuleException("sales.gift_card_invalid_amount", "El importe a canjear debe ser mayor que cero.");
        }
        if (amount > Balance)
        {
            throw new DomainRuleException("sales.gift_card_insufficient", "Saldo insuficiente en la tarjeta regalo.");
        }
        Balance -= amount;
        return new GiftCardTransaction
        {
            GiftCardId = Id,
            OrderId = orderId,
            Amount = -amount,
            TransactionTime = now,
        };
    }
}

public sealed class GiftCardTransaction
{
    public int Id { get; set; }
    public int GiftCardId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionTime { get; set; }
}

public interface ICustomerRepository
{
    Task<Customer?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> ListAsync(string? search, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    void Add(Customer customer);
}

public interface IGiftCardRepository
{
    Task<GiftCard?> GetAsync(int id, CancellationToken ct = default);
    void AddTransaction(GiftCardTransaction transaction);
}
