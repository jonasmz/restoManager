using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

/// <summary>Atajos para mapear el esquema de <c>restaurant_schema.sql</c>.</summary>
internal static class ConfigHelpers
{
    /// <summary>FK sin navegación, requerida por defecto, sin cascada (como el DDL).</summary>
    public static EntityTypeBuilder<TEntity> Fk<TEntity, TPrincipal>(
        this EntityTypeBuilder<TEntity> builder, string foreignKeyProperty, bool required = true)
        where TEntity : class
        where TPrincipal : class
    {
        builder.HasOne<TPrincipal>()
            .WithMany()
            .HasForeignKey(foreignKeyProperty)
            .IsRequired(required)
            .OnDelete(DeleteBehavior.NoAction);
        return builder;
    }

    /// <summary>Columna <c>decimal(p,s)</c>; por defecto <c>decimal(10,2)</c> como el DDL.</summary>
    public static PropertyBuilder<decimal> Money(this PropertyBuilder<decimal> p, int precision = 10, int scale = 2) =>
        p.HasPrecision(precision, scale);
}
