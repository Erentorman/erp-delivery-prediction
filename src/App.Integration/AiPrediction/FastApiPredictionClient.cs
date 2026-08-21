using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.Application.Contracts.Prediction;
using App.Application.IntegrationLogging;
using App.Application.Prediction;

namespace App.Integration.AiPrediction;

internal sealed class FastApiPredictionClient : IAiPredictionClient
{
    private const string Operation = "GetPrediction";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IIntegrationLogWriter _logWriter;
    private readonly TimeSpan _timeout;

    public FastApiPredictionClient(
        HttpClient httpClient,
        IIntegrationLogWriter logWriter,
        AiPredictionOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
        ArgumentNullException.ThrowIfNull(options);
        if (options.TimeoutMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AI prediction timeout must be positive.");
        }

        _timeout = TimeSpan.FromMilliseconds(options.TimeoutMs);
    }

    public async Task<AiPredictionResult> GetPredictionAsync(
        AiPredictionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Features);
        cancellationToken.ThrowIfCancellationRequested();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_timeout);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                AiPredictionContract.PredictionPath,
                MapRequest(request.Features),
                SerializerOptions,
                timeoutSource.Token);

            if (!response.IsSuccessStatusCode)
            {
                var status = MapHttpFailure(response.StatusCode);
                return await CompleteAsync(
                    AiPredictionResult.Failure(status, SafeMessage(status)),
                    (int)response.StatusCode,
                    stopwatch);
            }

            FastApiPredictionResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<FastApiPredictionResponse>(
                    SerializerOptions,
                    timeoutSource.Token);
            }
            catch (JsonException)
            {
                return await CompleteAsync(
                    AiPredictionResult.Failure(AiProviderStatus.InvalidResponse, "AI service returned an invalid response."),
                    (int)response.StatusCode,
                    stopwatch);
            }
            catch (NotSupportedException)
            {
                return await CompleteAsync(
                    AiPredictionResult.Failure(AiProviderStatus.InvalidResponse, "AI service returned an invalid response."),
                    (int)response.StatusCode,
                    stopwatch);
            }

            var result = Validate(payload);
            return await CompleteAsync(result, (int)response.StatusCode, stopwatch);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await CompleteAsync(
                AiPredictionResult.Failure(AiProviderStatus.Timeout, "AI prediction timed out."),
                null,
                stopwatch);
        }
        catch (OperationCanceledException)
        {
            await WriteLogWithoutMaskingAsync(null, stopwatch.ElapsedMilliseconds, false, "AI prediction was cancelled.");
            throw;
        }
        catch (HttpRequestException)
        {
            return await CompleteAsync(
                AiPredictionResult.Failure(AiProviderStatus.ServiceUnavailable, "AI service is unavailable."),
                null,
                stopwatch);
        }
    }

    internal static AiPredictionResult Validate(FastApiPredictionResponse? response)
    {
        if (response is null)
        {
            return AiPredictionResult.Failure(AiProviderStatus.InvalidResponse, "AI service returned an invalid response.");
        }

        if (string.IsNullOrWhiteSpace(response.ModelVersion) ||
            string.IsNullOrWhiteSpace(response.FeatureSchemaVersion) ||
            string.IsNullOrWhiteSpace(response.TrainingDatasetVersion))
        {
            return AiPredictionResult.Failure(AiProviderStatus.InvalidResponse, "AI response version metadata is missing.");
        }

        if (response.ModelVersion != AiPredictionContract.ModelVersion ||
            response.FeatureSchemaVersion != AiPredictionContract.FeatureSchemaVersion ||
            response.TrainingDatasetVersion != AiPredictionContract.TrainingDatasetVersion)
        {
            return AiPredictionResult.Failure(AiProviderStatus.VersionMismatch, "AI response version metadata is incompatible.");
        }

        if (response.WorkingLeadTimeMinutes is not double value)
        {
            return AiPredictionResult.Failure(AiProviderStatus.InvalidResponse, "AI response prediction is missing.");
        }

        if (!double.IsFinite(value) || value <= 0d)
        {
            return AiPredictionResult.Failure(AiProviderStatus.Rejected, "AI response prediction was rejected.");
        }

        return new AiPredictionResult(
            AiProviderStatus.Success,
            value,
            response.ModelVersion,
            response.FeatureSchemaVersion,
            response.TrainingDatasetVersion);
    }

    private async Task<AiPredictionResult> CompleteAsync(
        AiPredictionResult result,
        int? statusCode,
        Stopwatch stopwatch)
    {
        stopwatch.Stop();
        await WriteLogWithoutMaskingAsync(
            statusCode,
            stopwatch.ElapsedMilliseconds,
            result.Status == AiProviderStatus.Success,
            result.Status == AiProviderStatus.Success
                ? "AI prediction completed."
                : result.Message ?? "AI prediction failed.");
        return result;
    }

    private async Task WriteLogWithoutMaskingAsync(
        int? statusCode,
        long durationMs,
        bool isSuccess,
        string message)
    {
        try
        {
            await _logWriter.WriteAsync(
                new IntegrationLogRequest(
                    IntegrationType.Ai,
                    Operation,
                    AiPredictionContract.PredictionPath,
                    isSuccess,
                    statusCode,
                    Math.Max(0, durationMs),
                    message),
                CancellationToken.None);
        }
        catch
        {
            // An integration-log persistence failure must not replace the outbound result.
        }
    }

    private static AiProviderStatus MapHttpFailure(HttpStatusCode statusCode) =>
        (int)statusCode >= 500
            ? AiProviderStatus.ServiceUnavailable
            : AiProviderStatus.Rejected;

    private static string SafeMessage(AiProviderStatus status) => status switch
    {
        AiProviderStatus.ServiceUnavailable => "AI service is unavailable.",
        _ => "AI service rejected the request."
    };

    private static FastApiPredictionRequest MapRequest(AiFeaturePayload features) => new(
        features.FeatureSchemaVersion,
        features.ProductRef,
        features.ProductCategory,
        features.Quantity,
        features.BomItemCount,
        features.MissingMaterialCount,
        features.TotalMissingQuantity,
        features.MaximumSupplierLeadTimeDays,
        features.OperationCount,
        features.TotalStandardOperationMinutes,
        features.WorkCenterLoadRatio,
        features.ActiveWorkOrderCount,
        features.ShiftCapacityMinutes,
        features.HolidayCount,
        features.PlannedDowntimeMinutes,
        features.ShippingDurationMinutes,
        features.RequestedDeliveryLeadMinutes);
}
