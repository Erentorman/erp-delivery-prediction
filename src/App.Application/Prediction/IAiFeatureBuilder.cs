using App.Application.Contracts.Prediction;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public interface IAiFeatureBuilder
{
    AiFeaturePayload Build(PredictionContext context);
}
