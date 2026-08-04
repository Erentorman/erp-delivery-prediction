using App.Domain.Abstractions;
using App.Infrastructure.Clock;
using FluentAssertions;

namespace App.Infrastructure.Tests.Clock;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsCurrentUtcTime()
    {
        var clock = new SystemClock();
        var before = DateTimeOffset.UtcNow;

        var result = clock.UtcNow;

        var after = DateTimeOffset.UtcNow;
        result.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UtcNow_HasZeroOffset()
    {
        var clock = new SystemClock();

        var result = clock.UtcNow;

        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void SystemClock_ImplementsIClock()
    {
        IClock clock = new SystemClock();

        clock.UtcNow.Offset.Should().Be(TimeSpan.Zero);
    }
}
