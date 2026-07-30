namespace App.Application.IntegrationLogging;

public sealed record IntegrationLogRequest(
    IntegrationType IntegrationType,
    string Operation,
    string ExternalResource,
    bool IsSuccess,
    int? StatusCode,
    long DurationMs,
    string? Message);
