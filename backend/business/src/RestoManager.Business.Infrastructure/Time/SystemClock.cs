using RestoManager.Business.Domain.Abstractions;

namespace RestoManager.Business.Infrastructure.Time;

/// <summary>
/// Devuelve la hora en valor UTC con <see cref="DateTimeKind.Unspecified"/>, que es
/// lo que Npgsql exige para las columnas <c>timestamp without time zone</c> del
/// esquema (`restaurant_schema.sql`).
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}
