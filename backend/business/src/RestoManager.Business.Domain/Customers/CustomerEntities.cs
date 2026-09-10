using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Customers;

// Módulo Cliente. La Fase 5 añadió un alta rápida mínima (nombre + contacto) para
// poder crear reservas antes de la Fase 8. La Fase 8 se hace dueña del módulo:
// fidelización (loyalty_points), gift cards (emisión + saldo) y reseñas.
// gift_card_transactions se materializó en la Fase 6 como medio de pago.

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

    /// <summary>
    /// Acumulación de fidelidad (Fase 8, decisión 1): la dispara <c>CloseOrder</c> con
    /// <c>floor(total)</c> puntos si el pedido tiene cliente. Nunca resta.
    /// </summary>
    public void AddLoyaltyPoints(int points)
    {
        if (points < 0)
        {
            throw new DomainRuleException(
                "customers.invalid_points", "Los puntos a acumular no pueden ser negativos.");
        }
        LoyaltyPoints += points;
    }

    /// <summary>Canje de fidelidad: descuenta puntos del saldo del cliente (Fase 8).</summary>
    public void RedeemLoyaltyPoints(int points)
    {
        if (points <= 0)
        {
            throw new DomainRuleException(
                "customers.invalid_points", "Los puntos a canjear deben ser mayores que cero.");
        }
        if (points > LoyaltyPoints)
        {
            throw new DomainRuleException(
                "loyalty.insufficient_points",
                $"El cliente solo tiene {LoyaltyPoints} puntos de fidelidad.");
        }
        LoyaltyPoints -= points;
    }

    private static string Required(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainRuleException("customers.missing_field", $"El {field} del cliente es obligatorio.")
            : value.Trim();
}

/// <summary>Reseña de un cliente sobre una sucursal (rating 1–5, comentario opcional).</summary>
public sealed class Review
{
    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public int BranchId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateOnly ReviewDate { get; private set; }

    private Review() { }

    public static Review Create(int customerId, int branchId, int rating, string? comment, DateOnly reviewDate)
    {
        if (customerId <= 0)
        {
            throw new DomainRuleException("reviews.invalid_customer", "La reseña debe ir ligada a un cliente.");
        }
        if (branchId <= 0)
        {
            throw new DomainRuleException("reviews.invalid_branch", "La reseña debe ir ligada a una sucursal.");
        }
        if (rating is < 1 or > 5)
        {
            throw new DomainRuleException("reviews.invalid_rating", "La valoración debe estar entre 1 y 5.");
        }

        var text = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (text is { Length: > 500 })
        {
            throw new DomainRuleException("reviews.comment_too_long", "El comentario no puede superar los 500 caracteres.");
        }

        return new Review
        {
            CustomerId = customerId,
            BranchId = branchId,
            Rating = rating,
            Comment = text,
            ReviewDate = reviewDate,
        };
    }
}

/// <summary>
/// Tarjeta regalo. La emisión y el control de saldo son de la Fase 8; el canje como
/// medio de pago de un pedido se implementó en la Fase 6. Sin recarga (decisión 2):
/// el saldo solo baja por <see cref="Redeem"/>.
/// </summary>
public sealed class GiftCard
{
    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public string CardNumber { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public DateOnly ExpiryDate { get; private set; }

    private GiftCard() { }

    /// <summary>Emisión (Fase 8): a un cliente, con saldo inicial &gt; 0 y caducidad futura.</summary>
    public static GiftCard Issue(
        int customerId, string cardNumber, decimal initialBalance, DateOnly expiryDate, DateOnly today)
    {
        if (customerId <= 0)
        {
            throw new DomainRuleException("gift_cards.invalid_customer", "La tarjeta regalo debe emitirse a un cliente.");
        }
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            throw new DomainRuleException("gift_cards.card_number_required", "El número de tarjeta es obligatorio.");
        }
        if (initialBalance <= 0m)
        {
            throw new DomainRuleException("gift_cards.invalid_balance", "El saldo inicial debe ser mayor que cero.");
        }
        if (expiryDate < today)
        {
            throw new DomainRuleException("gift_cards.invalid_expiry", "La fecha de caducidad no puede ser anterior a hoy.");
        }

        return new GiftCard
        {
            CustomerId = customerId,
            CardNumber = cardNumber.Trim(),
            Balance = Money.Round(initialBalance),
            ExpiryDate = expiryDate,
        };
    }

    public bool IsExpiredOn(DateOnly date) => ExpiryDate < date;

    /// <summary>
    /// Canje de la tarjeta como medio de pago de un pedido (Fase 6b). Descuenta
    /// <paramref name="amount"/> del saldo y devuelve el asiento (<c>amount</c>
    /// negativo). Rechaza tarjeta caducada (Fase 8, decisión 2).
    /// </summary>
    public GiftCardTransaction Redeem(int orderId, decimal amount, DateTime now)
    {
        if (IsExpiredOn(DateOnly.FromDateTime(now)))
        {
            throw new DomainRuleException("sales.gift_card_expired", "La tarjeta regalo está caducada.");
        }
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
    Task<GiftCard?> GetByCardNumberAsync(string cardNumber, CancellationToken ct = default);
    Task<bool> ExistsByCardNumberAsync(string cardNumber, CancellationToken ct = default);
    Task<IReadOnlyList<GiftCard>> ListForCustomerAsync(int customerId, CancellationToken ct = default);
    void Add(GiftCard giftCard);
    void AddTransaction(GiftCardTransaction transaction);
}

public interface IReviewRepository
{
    Task<IReadOnlyList<Review>> ListForBranchAsync(
        int branchId, int? minRating, int skip, int take, CancellationToken ct = default);
    Task<int> CountForBranchAsync(int branchId, int? minRating, CancellationToken ct = default);
    void Add(Review review);
}
