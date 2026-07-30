using App.Application.IntegrationLogging;
using App.Domain.Entities;
using App.Persistence;
using App.Persistence.IntegrationLogging;
using Microsoft.EntityFrameworkCore;

namespace App.Integration.Tests.Persistence;

public class IntegrationLogWriterTests
{
    [Fact]
    public async Task WriteAsync_TracksOneIntegrationLogAndOneSafeAuditLog()
    {
        await using var context = CreateContext();
        var writer = new IntegrationLogWriter(context);
        var request = CreateRequest(
            IntegrationType.Erp,
            message: "External call completed");

        await writer.WriteAsync(request);

        var integrationLog = Assert.Single(context.ChangeTracker.Entries<IntegrationLog>()).Entity;
        Assert.Equal("Erp", integrationLog.IntegrationType);
        Assert.Equal(request.Operation, integrationLog.Operation);
        Assert.Equal(request.ExternalResource, integrationLog.ExternalResource);
        Assert.Equal(request.IsSuccess, integrationLog.IsSuccess);
        Assert.Equal(request.StatusCode, integrationLog.StatusCode);
        Assert.Equal(request.DurationMs, integrationLog.DurationMs);
        Assert.Equal(request.Message, integrationLog.Message);

        var auditLog = Assert.Single(context.ChangeTracker.Entries<AuditLog>()).Entity;
        Assert.Equal("Erp", auditLog.IntegrationType);
        Assert.Equal(request.Operation, auditLog.Operation);
        Assert.Equal(request.IsSuccess, auditLog.IsSuccess);
        Assert.Same(integrationLog, auditLog.IntegrationLog);
        Assert.Equal(integrationLog.CreatedAt, auditLog.CreatedAt);

        Assert.Equal(2, context.ChangeTracker.Entries().Count());
        Assert.Equal(1, context.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(IntegrationType.Erp, "Erp")]
    [InlineData(IntegrationType.Ai, "Ai")]
    public async Task WriteAsync_PersistsSupportedIntegrationType(
        IntegrationType integrationType,
        string expectedValue)
    {
        await using var context = CreateContext();
        var writer = new IntegrationLogWriter(context);

        await writer.WriteAsync(CreateRequest(integrationType));

        var integrationLog = Assert.Single(context.ChangeTracker.Entries<IntegrationLog>()).Entity;
        var auditLog = Assert.Single(context.ChangeTracker.Entries<AuditLog>()).Entity;
        Assert.Equal(expectedValue, integrationLog.IntegrationType);
        Assert.Equal(expectedValue, auditLog.IntegrationType);
    }

    [Fact]
    public async Task WriteAsync_PassesCancellationTokenToSingleSave()
    {
        await using var context = CreateContext();
        var writer = new IntegrationLogWriter(context);
        using var cancellationSource = new CancellationTokenSource();

        await writer.WriteAsync(CreateRequest(IntegrationType.Ai), cancellationSource.Token);

        Assert.Equal(1, context.SaveChangesCallCount);
        Assert.Equal(cancellationSource.Token, context.LastCancellationToken);
    }

    [Fact]
    public async Task WriteAsync_WithNegativeDuration_RejectsWithoutSaving()
    {
        await using var context = CreateContext();
        var writer = new IntegrationLogWriter(context);
        var request = CreateRequest(IntegrationType.Erp) with { DurationMs = -1 };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => writer.WriteAsync(request));

        Assert.Equal(0, context.SaveChangesCallCount);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    private static IntegrationLogRequest CreateRequest(
        IntegrationType integrationType,
        string? message = null) =>
        new(
            integrationType,
            "SynchronizeOrders",
            "orders",
            true,
            200,
            125,
            message);

    private static TestAppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=change_tracker_only")
            .Options;

        return new TestAppDbContext(options);
    }

    private sealed class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public int SaveChangesCallCount { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(ChangeTracker.Entries().Count());
        }
    }
}
