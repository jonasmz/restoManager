using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfig : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(100).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
    }
}

internal sealed class ReviewConfig : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.Property(x => x.Comment).HasMaxLength(500);
        b.ToTable(t => t.HasCheckConstraint("ck_reviews_rating", "rating BETWEEN 1 AND 5"));
        b.Fk<Review, Customer>(nameof(Review.CustomerId));
        b.Fk<Review, Branch>(nameof(Review.BranchId));
    }
}

internal sealed class GiftCardConfig : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> b)
    {
        b.Property(x => x.CardNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.Balance).Money();
        b.HasIndex(x => x.CardNumber).IsUnique();
        b.Fk<GiftCard, Customer>(nameof(GiftCard.CustomerId));
    }
}

internal sealed class GiftCardTransactionConfig : IEntityTypeConfiguration<GiftCardTransaction>
{
    public void Configure(EntityTypeBuilder<GiftCardTransaction> b)
    {
        b.Property(x => x.Amount).Money();
        b.Fk<GiftCardTransaction, GiftCard>(nameof(GiftCardTransaction.GiftCardId));
        b.Fk<GiftCardTransaction, Order>(nameof(GiftCardTransaction.OrderId));
    }
}
