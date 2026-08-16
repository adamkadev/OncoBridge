namespace OncoBridge.Infrastructure.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    internal void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
}
