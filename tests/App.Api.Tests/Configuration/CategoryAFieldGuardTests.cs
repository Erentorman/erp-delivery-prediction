using System;
using System.Collections.Generic;
using App.Api.Configuration;
using App.Application.Contracts.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace App.Api.Tests.Configuration;

public class CategoryAFieldGuardTests
{
    private class FakeLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Logs { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => throw new NotImplementedException();
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logs.Add((logLevel, formatter(state, exception)));
        }
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldNotThrow()
    {
        // Arrange
        var json = """{"mvpAssumptions": {}}""";
        var options = new MvpAssumptionsOptions
        {
            Shipping = new ShippingAssumptionsOptions { FallbackDurationMinutes = 10 }
        };
        var logger = new FakeLogger();

        // Act & Assert
        CategoryAFieldGuard.Validate(json, options, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Validate_WithMissingShippingFallback_ShouldLogWarning()
    {
        // Arrange
        var json = """{"mvpAssumptions": {}}""";
        var options = new MvpAssumptionsOptions
        {
            Shipping = new ShippingAssumptionsOptions { FallbackDurationMinutes = null }
        };
        var logger = new FakeLogger();

        // Act
        CategoryAFieldGuard.Validate(json, options, logger);

        // Assert
        Assert.Contains(logger.Logs, l => l.Level == LogLevel.Warning && l.Message.Contains("shipping.fallbackDurationMinutes is null"));
    }

    [Theory]
    [InlineData("machineCount")]
    [InlineData("defaultMachineCount")]
    [InlineData("defaultMachineCountPerWorkCenter")]
    [InlineData("operationDuration")]
    [InlineData("operationDurationFallback")]
    [InlineData("durationBasis")]
    [InlineData("durationBasisFallback")]
    [InlineData("alternativeWorkCenter")]
    [InlineData("alternativeWorkCenterPriority")]
    public void Validate_WithCategoryAField_ShouldThrow(string forbiddenField)
    {
        // Arrange
        var json = $$"""
        {
            "mvpAssumptions": {
            "someNested": {
                "{{forbiddenField}}": 5
            }
            }
        }
        """;
        var options = new MvpAssumptionsOptions();
        var logger = new FakeLogger();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => CategoryAFieldGuard.Validate(json, options, logger));
        Assert.Contains("Forbidden Category A field", ex.Message);
    }
}
