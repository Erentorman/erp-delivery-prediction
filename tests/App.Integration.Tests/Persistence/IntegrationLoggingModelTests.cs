using App.Domain.Entities;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace App.Integration.Tests.Persistence;

public class IntegrationLoggingModelTests
{
    private readonly IModel _model = CreateContext().Model;

    [Fact]
    public void Model_MapsIntegrationAndAuditLogsToCommonTables()
    {
        var integrationLog = GetEntity<IntegrationLog>();
        var auditLog = GetEntity<AuditLog>();

        Assert.Equal("IntegrationLogs", integrationLog.GetTableName());
        Assert.Equal("AuditLogs", auditLog.GetTableName());
        Assert.Equal("integration_type", integrationLog.FindProperty(nameof(IntegrationLog.IntegrationType))!.GetColumnName());
        Assert.Equal("integration_log_id", auditLog.FindProperty(nameof(AuditLog.IntegrationLogId))!.GetColumnName());
    }

    [Fact]
    public void IntegrationLog_HasExpectedConstraintsAndIndexes()
    {
        var entity = GetEntity<IntegrationLog>();

        AssertProperty(entity, nameof(IntegrationLog.IntegrationType), false, 20);
        AssertProperty(entity, nameof(IntegrationLog.Operation), false, 100);
        AssertProperty(entity, nameof(IntegrationLog.ExternalResource), false, 250);
        AssertProperty(entity, nameof(IntegrationLog.Message), true, 1000);
        Assert.True(entity.FindProperty(nameof(IntegrationLog.StatusCode))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(IntegrationLog.DurationMs))!.IsNullable);

        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(IntegrationLog.IntegrationType) }));
        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[]
                {
                    nameof(IntegrationLog.IntegrationType),
                    nameof(IntegrationLog.Operation)
                }));
    }

    [Fact]
    public void AuditLog_ContainsOnlySafeMetadataAndHasOneToOneRelationship()
    {
        var entity = GetEntity<AuditLog>();
        var expectedProperties = new[]
        {
            nameof(AuditLog.Id),
            nameof(AuditLog.IntegrationLogId),
            nameof(AuditLog.IntegrationType),
            nameof(AuditLog.Operation),
            nameof(AuditLog.IsSuccess),
            nameof(AuditLog.CreatedAt)
        };

        Assert.Equal(expectedProperties.Order(), entity.GetProperties().Select(p => p.Name).Order());
        AssertProperty(entity, nameof(AuditLog.IntegrationType), false, 20);
        AssertProperty(entity, nameof(AuditLog.Operation), false, 100);

        var foreignKey = Assert.Single(entity.GetForeignKeys());
        Assert.True(foreignKey.IsUnique);
        Assert.Equal(typeof(IntegrationLog), foreignKey.PrincipalEntityType.ClrType);
    }

    [Fact]
    public void PersistenceEntities_DoNotContainPayloadOrSecretProperties()
    {
        var forbiddenTerms = new[]
        {
            "Payload", "RequestBody", "ResponseBody", "Password",
            "Token", "Authorization", "ConnectionString"
        };
        var propertyNames = new[] { GetEntity<IntegrationLog>(), GetEntity<AuditLog>() }
            .SelectMany(entity => entity.GetProperties())
            .Select(property => property.Name);

        Assert.DoesNotContain(
            propertyNames,
            name => forbiddenTerms.Any(term =>
                name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private IEntityType GetEntity<TEntity>() =>
        _model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is missing from the model.");

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only")
            .Options;

        return new AppDbContext(options);
    }

    private static void AssertProperty(
        IEntityType entity,
        string propertyName,
        bool isNullable,
        int maxLength)
    {
        var property = entity.FindProperty(propertyName)
            ?? throw new InvalidOperationException($"{propertyName} is missing.");

        Assert.Equal(isNullable, property.IsNullable);
        Assert.Equal(maxLength, property.GetMaxLength());
    }
}
