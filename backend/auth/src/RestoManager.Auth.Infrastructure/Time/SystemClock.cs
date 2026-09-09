using RestoManager.Auth.Domain.Abstractions;

namespace RestoManager.Auth.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
