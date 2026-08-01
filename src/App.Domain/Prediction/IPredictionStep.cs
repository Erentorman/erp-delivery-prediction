namespace App.Domain.Prediction;

/// <summary>
/// Common contract implemented by each Step-Based Pipeline unit (SAD §11.1). A step takes
/// the shared PredictionContext and enriches it in place; it never creates a new instance.
/// </summary>
public interface IPredictionStep
{
    void Execute(PredictionContext context);
}
