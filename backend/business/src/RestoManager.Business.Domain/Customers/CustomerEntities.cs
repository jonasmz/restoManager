namespace RestoManager.Business.Domain.Customers;

// Módulo Cliente. Solo esquema en la Fase 3. La Fase 8 añade fidelización, gift
// cards y reseñas. gift_card_transactions se materializa en la Fase 6 (pago).

public sealed class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; }
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
    public decimal Balance { get; set; }
    public DateOnly ExpiryDate { get; set; }
}

public sealed class GiftCardTransaction
{
    public int Id { get; set; }
    public int GiftCardId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionTime { get; set; }
}
