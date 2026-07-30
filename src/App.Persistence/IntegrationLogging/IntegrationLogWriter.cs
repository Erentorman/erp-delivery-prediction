using App.Application.IntegrationLogging;
using App.Domain.Entities;

namespace App.Persistence.IntegrationLogging;

public sealed class IntegrationLogWriter : IIntegrationLogWriter
{
    private const int MaxOperationLength = 100;
    private const int MaxExternalResourceLength = 250;
    private const int MaxMessageLength = 1000;

    private readonly AppDbContext _dbContext;

    public IntegrationLogWriter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(
        IntegrationLogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var createdAt = DateTime.UtcNow;
        var integrationType = request.IntegrationType.ToString();
        var integrationLog = new IntegrationLog
        {
            IntegrationType = integrationType,
            Operation = request.Operation,
            ExternalResource = request.ExternalResource,
            IsSuccess = request.IsSuccess,
            StatusCode = request.StatusCode,
            DurationMs = request.DurationMs,
            Message = request.Message,
            CreatedAt = createdAt
        };

        var auditLog = new AuditLog
        {
            IntegrationLog = integrationLog,
            IntegrationType = integrationType,
            Operation = request.Operation,
            IsSuccess = request.IsSuccess,
            CreatedAt = createdAt
        };

        _dbContext.IntegrationLogs.Add(integrationLog);
        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(IntegrationLogRequest request)
    {
        if (!Enum.IsDefined(request.IntegrationType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.IntegrationType),
                request.IntegrationType,
                "Integration type is not supported.");
        }

        ValidateRequiredText(request.Operation, nameof(request.Operation), MaxOperationLength);
        ValidateRequiredText(
            request.ExternalResource,
            nameof(request.ExternalResource),
            MaxExternalResourceLength);

        if (request.DurationMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.DurationMs),
                request.DurationMs,
                "Duration cannot be negative.");
        }

        if (request.StatusCode < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.StatusCode),
                request.StatusCode,
                "Status code cannot be negative.");
        }

        if (request.Message?.Length > MaxMessageLength)
        {
            throw new ArgumentException(
                $"Message cannot exceed {MaxMessageLength} characters.",
                nameof(request.Message));
        }
    }

    private static void ValidateRequiredText(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }
    }
}
