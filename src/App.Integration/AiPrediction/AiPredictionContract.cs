namespace App.Integration.AiPrediction;

internal static class AiPredictionContract
{
    public const string PredictionPath = "predict";
    public const string ModelVersion = "xgb-v0.1";
    public const string FeatureSchemaVersion = "1";
    public const string TrainingDatasetVersion = "synthetic-v1";
}
