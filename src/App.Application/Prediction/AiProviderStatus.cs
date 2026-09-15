namespace App.Application.Prediction;

public enum AiProviderStatus
{
    Success = 1,
    Timeout = 2,
    ServiceUnavailable = 3,
    InvalidResponse = 4,
    InsufficientFeatures = 5,
    ModelUnavailable = 6,
    VersionMismatch = 7,
    Rejected = 8
}
