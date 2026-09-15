namespace App.Application.Prediction;

public interface IFinalPredictionCombiner
{
    FinalPredictionResult Combine(PredictionProviderResult ruleBased, PredictionProviderResult ai);
}
