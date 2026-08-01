namespace App.Application.IntegrationLogging;

public interface IIntegrationLogWriter
{
    Task WriteAsync(
        IntegrationLogRequest request,
        CancellationToken cancellationToken = default);
}
