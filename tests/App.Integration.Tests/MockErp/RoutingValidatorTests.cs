using MockErp.Api.Models;
using MockErp.Api.Validation;

namespace App.Integration.Tests.MockErp;

public sealed class RoutingValidatorTests
{
    [Fact]
    public void EmptyRoutingIsValid()
    {
        RoutingValidator.Validate(new MockErpRouting("ROUTE-EMPTY", []));
    }

    [Fact]
    public void PositiveDurationAndKnownPredecessorAreValid()
    {
        var routing = new MockErpRouting(
            "ROUTE-VALID",
            [
                Operation("OP-10", 10, 30),
                Operation("OP-20", 20, 45, "OP-10")
            ]);

        RoutingValidator.Validate(routing);
    }

    [Fact]
    public void PositiveOperationSequenceIsValid()
    {
        RoutingValidator.Validate(
            new MockErpRouting("ROUTE-VALID", [Operation("OP-10", 1, 30)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveDurationIsInvalid(long duration)
    {
        var routing = new MockErpRouting("ROUTE-INVALID", [Operation("OP-10", 10, duration)]);

        var exception = Assert.Throws<InvalidOperationException>(() => RoutingValidator.Validate(routing));

        Assert.Contains("StandardDurationMinutes", exception.Message);
        Assert.Contains("OP-10", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveOperationSequenceIsInvalid(int sequence)
    {
        var routing = new MockErpRouting(
            "ROUTE-INVALID",
            [Operation("OP-10", sequence, 30)]);

        var exception = Assert.Throws<InvalidOperationException>(() => RoutingValidator.Validate(routing));

        Assert.Contains("OperationSequence", exception.Message);
        Assert.Contains(sequence.ToString(), exception.Message);
        Assert.Contains("OP-10", exception.Message);
    }

    [Fact]
    public void DirectSelfPredecessorIsInvalid()
    {
        var routing = new MockErpRouting(
            "ROUTE-INVALID",
            [Operation("OP-10", 10, 30, "OP-10")]);

        var exception = Assert.Throws<InvalidOperationException>(() => RoutingValidator.Validate(routing));

        Assert.Contains("cannot list itself as a predecessor", exception.Message);
        Assert.Contains("OP-10", exception.Message);
    }

    [Fact]
    public void UnknownPredecessorIsInvalid()
    {
        var routing = new MockErpRouting(
            "ROUTE-INVALID",
            [Operation("OP-20", 20, 45, "OP-OTHER")]);

        var exception = Assert.Throws<InvalidOperationException>(() => RoutingValidator.Validate(routing));

        Assert.Contains("OP-OTHER", exception.Message);
        Assert.Contains("same routing", exception.Message);
    }

    [Fact]
    public void PredecessorFromAnotherRoutingIsInvalid()
    {
        var first = new MockErpRouting("ROUTE-1", [Operation("OP-10", 10, 30)]);
        var second = new MockErpRouting("ROUTE-2", [Operation("OP-20", 20, 45, "OP-10")]);

        RoutingValidator.Validate(first);
        var exception = Assert.Throws<InvalidOperationException>(() => RoutingValidator.Validate(second));

        Assert.Contains("ROUTE-2", exception.Message);
        Assert.Contains("OP-10", exception.Message);
    }

    [Fact]
    public void DuplicateOperationReferenceIsInvalid()
    {
        var routing = new MockErpRouting(
            "ROUTE-DUPLICATE",
            [Operation("OP-10", 10, 30), Operation("OP-10", 20, 30)]);

        Assert.Contains(
            "duplicate operation reference",
            Assert.Throws<InvalidOperationException>(() => RoutingValidator.Validate(routing)).Message);
    }

    [Fact]
    public void DuplicateOperationSequenceIsInvalid()
    {
        var routing = new MockErpRouting(
            "ROUTE-DUPLICATE",
            [Operation("OP-10", 10, 30), Operation("OP-20", 10, 30)]);

        Assert.Contains(
            "duplicate operation sequence",
            Assert.Throws<InvalidOperationException>(() => RoutingValidator.Validate(routing)).Message);
    }

    private static MockErpOperation Operation(
        string reference,
        int sequence,
        long duration,
        params string[] predecessors) =>
        new(reference, sequence, "WC-ASSEMBLY-01", duration, predecessors);
}
