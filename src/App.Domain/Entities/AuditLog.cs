namespace App.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public long IntegrationLogId { get; set; }
    public string IntegrationType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public DateTime CreatedAt { get; set; }

    public IntegrationLog IntegrationLog { get; set; } = null!;
}
