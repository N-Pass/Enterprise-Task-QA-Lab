namespace TaskManagement.IntegrationTests.Infrastructure;

/// <summary>Clock controlled by the test, injected into the API instead of TimeProvider.System.</summary>
public sealed class TestClock : TimeProvider
{
    private DateTimeOffset _utcNow;

    public TestClock(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
}
