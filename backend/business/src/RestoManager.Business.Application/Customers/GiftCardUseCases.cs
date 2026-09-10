using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Customers;

namespace RestoManager.Business.Application.Customers;

// Emisión y consulta de saldo de gift cards (Fase 8). El canje como medio de pago
// vive en la Fase 6 (RegisterPayment con method=GIFT_CARD). Sin recarga (decisión 2).

public sealed record GiftCardDto(
    int Id, int CustomerId, string CardNumber, decimal Balance, DateOnly ExpiryDate, bool Expired);

// ---- Emisión ----

public sealed record IssueGiftCardCommand(
    int CustomerId, string CardNumber, decimal InitialBalance, DateOnly ExpiryDate);

public sealed class IssueGiftCardValidator : AbstractValidator<IssueGiftCardCommand>
{
    public IssueGiftCardValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.CardNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.InitialBalance).GreaterThan(0);
    }
}

public sealed class IssueGiftCardHandler(
    IGiftCardRepository giftCards,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<IssueGiftCardCommand> validator)
{
    public async Task<int> HandleAsync(IssueGiftCardCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        if (!await customers.ExistsAsync(command.CustomerId, ct))
        {
            throw new NotFoundException("cliente", command.CustomerId);
        }

        var cardNumber = command.CardNumber.Trim();
        if (await giftCards.ExistsByCardNumberAsync(cardNumber, ct))
        {
            throw new DomainRuleException(
                "gift_cards.duplicate_card_number", $"Ya existe una tarjeta regalo con el número '{cardNumber}'.");
        }

        var card = GiftCard.Issue(
            command.CustomerId, cardNumber, command.InitialBalance, command.ExpiryDate,
            DateOnly.FromDateTime(clock.UtcNow));
        giftCards.Add(card);
        await unitOfWork.SaveChangesAsync(ct);
        return card.Id;
    }
}

// ---- Consultas ----

public sealed class GetGiftCardBalanceHandler(IGiftCardRepository giftCards, IClock clock)
{
    public async Task<GiftCardDto> HandleAsync(string cardNumber, CancellationToken ct = default)
    {
        var card = await giftCards.GetByCardNumberAsync(cardNumber.Trim(), ct)
            ?? throw new NotFoundException("tarjeta regalo", cardNumber);
        return card.Map(DateOnly.FromDateTime(clock.UtcNow));
    }
}

public sealed class ListCustomerGiftCardsHandler(
    IGiftCardRepository giftCards, ICustomerRepository customers, IClock clock)
{
    public async Task<IReadOnlyList<GiftCardDto>> HandleAsync(int customerId, CancellationToken ct = default)
    {
        if (!await customers.ExistsAsync(customerId, ct))
        {
            throw new NotFoundException("cliente", customerId);
        }
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var cards = await giftCards.ListForCustomerAsync(customerId, ct);
        return cards.Select(c => c.Map(today)).ToList();
    }
}

internal static class GiftCardMapping
{
    public static GiftCardDto Map(this GiftCard c, DateOnly today) =>
        new(c.Id, c.CustomerId, c.CardNumber, c.Balance, c.ExpiryDate, c.IsExpiredOn(today));
}
