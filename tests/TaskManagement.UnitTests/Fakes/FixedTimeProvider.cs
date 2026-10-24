namespace TaskManagement.UnitTests.Fakes;

/// <summary>Clock controlled by the test, so timestamps are deterministic.</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
}
