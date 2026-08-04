using App.Domain.Abstractions;

namespace App.Domain.Tests.Abstractions;

public class FixedClockTests
{
    [Fact]
    public void UtcNow_ReturnsTheSuppliedInstant()
    {
        var instant = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);

        var clock = new FixedClock(instant);

        Assert.Equal(instant, clock.UtcNow);
    }

    [Fact]
    public void UtcNow_DoesNotChangeAcrossMultipleReads()
    {
        var instant = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(instant);

        var first = clock.UtcNow;
        var second = clock.UtcNow;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Constructor_WithNonUtcOffset_ThrowsArgumentException()
    {
        var nonUtc = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.FromHours(3));

        Assert.Throws<ArgumentException>(() => new FixedClock(nonUtc));
    }

    [Fact]
    public void FixedClock_ImplementsIClock()
    {
        var instant = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);

        IClock clock = new FixedClock(instant);

        Assert.Equal(instant, clock.UtcNow);
    }

    [Fact]
    public void Advance_IncreasesUtcNowByGivenAmount()
    {
        var instant = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(instant);

        clock.Advance(TimeSpan.FromMinutes(15));

        var expected = new DateTimeOffset(2026, 7, 1, 8, 15, 0, TimeSpan.Zero);
        Assert.Equal(expected, clock.UtcNow);
    }
}
