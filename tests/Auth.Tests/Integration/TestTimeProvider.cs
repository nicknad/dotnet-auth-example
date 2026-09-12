namespace Auth.Tests.Integration;

/// <summary>
/// A controllable <see cref="TimeProvider"/> so tests can advance the clock.
/// </summary>
#pragma warning disable CA1515 // Consider making public types internal
public sealed class TestTimeProvider : TimeProvider
#pragma warning restore CA1515 // Consider making public types internal
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
}
