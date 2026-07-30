namespace App.Domain.Entities;

public class IntegrationLog
{
    public long Id { get; set; }
    public string IntegrationType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string ExternalResource { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public int? StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }

    public AuditLog AuditLog { get; set; } = null!;
}
