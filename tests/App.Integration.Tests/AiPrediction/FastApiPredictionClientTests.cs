using System.Net;
using System.Text;
using System.Text.Json;
using App.Application.Contracts.Prediction;
using App.Application.IntegrationLogging;
using App.Application.Prediction;
using App.Integration.AiPrediction;

namespace App.Integration.Tests.AiPrediction;

public sealed class FastApiPredictionClientTests
{
    [Fact]
    public async Task ValidResponse_ReturnsSuccessAndPreservesContract()
    {
        var handler = new RecordingHandler(Json(SuccessPayload()));
        var (client, logs) = Create(handler);

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.Success, result.Status);
        Assert.Equal(4320d, result.WorkingLeadTimeMinutes);
        Assert.Equal("xgb-v0.1", result.ModelVersion);
        Assert.Equal("1", result.FeatureSchemaVersion);
        Assert.Equal("synthetic-v1", result.TrainingDatasetVersion);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal("/predict", sent.Uri.AbsolutePath);
        using var document = JsonDocument.Parse(sent.Body!);
        Assert.Equal(17, document.RootElement.EnumerateObject().Count());
        Assert.Equal(1, document.RootElement.GetProperty("featureSchemaVersion").GetInt32());
        Assert.Equal("SENTINEL-PRODUCT-908", document.RootElement.GetProperty("productRef").GetString());
        Assert.Equal(987654.321m, document.RootElement.GetProperty("quantity").GetDecimal());
        var log = Assert.Single(logs.Requests);
        Assert.True(log.IsSuccess);
        Assert.Equal(IntegrationType.Ai, log.IntegrationType);
        Assert.Equal("GetPrediction", log.Operation);
        Assert.Equal("predict", log.ExternalResource);
        Assert.Equal(200, log.StatusCode);
    }

    [Fact]
    public async Task ConfiguredTimeout_ReturnsTimeoutAndUsesInjectedValue()
    {
        var handler = new HangingHandler();
        var (client, logs) = Create(handler, timeoutMs: 25);
        var started = DateTime.UtcNow;

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.Timeout, result.Status);
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(2));
        Assert.True(handler.ObservedToken.CanBeCanceled);
        Assert.False(Assert.Single(logs.Requests).IsSuccess);
    }

    [Fact]
    public async Task CallerCancellation_IsPropagatedAndNotReportedAsTimeout()
    {
        var handler = new HangingHandler();
        var (client, logs) = Create(handler, timeoutMs: 10_000);
        using var source = new CancellationTokenSource(25);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetPredictionAsync(Request(), source.Token));

        Assert.True(handler.ObservedToken.CanBeCanceled);
        var log = Assert.Single(logs.Requests);
        Assert.Contains("cancelled", log.Message!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("timed out", log.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Http5xx_ReturnsControlledServiceUnavailable(HttpStatusCode statusCode)
    {
        var (client, logs) = Create(new RecordingHandler(new HttpResponseMessage(statusCode)));

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.ServiceUnavailable, result.Status);
        Assert.Equal((int)statusCode, Assert.Single(logs.Requests).StatusCode);
    }

    [Fact]
    public async Task TransportFailure_ReturnsServiceUnavailableWithoutLeakingDetails()
    {
        const string secret = "transport-secret-908";
        var (client, logs) = Create(new RecordingHandler(new HttpRequestException(secret)));

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.ServiceUnavailable, result.Status);
        Assert.DoesNotContain(secret, result.Message!);
        Assert.DoesNotContain(logs.Requests, x => x.Message?.Contains(secret, StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task InvalidJson_ReturnsInvalidResponseWithoutRawBodyLeak()
    {
        const string rawBody = "raw-response-secret-908";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
        };
        var (client, logs) = Create(new RecordingHandler(response));

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.InvalidResponse, result.Status);
        Assert.DoesNotContain(rawBody, result.Message!);
        Assert.DoesNotContain(logs.Requests, x => x.Message?.Contains(rawBody, StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task MissingPrediction_ReturnsInvalidResponse()
    {
        var (client, _) = Create(new RecordingHandler(Json(SuccessPayload() with { WorkingLeadTimeMinutes = null })));

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.InvalidResponse, result.Status);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(0d)]
    public async Task NonPositivePrediction_ReturnsRejected(double value)
    {
        var (client, _) = Create(new RecordingHandler(Json(SuccessPayload() with { WorkingLeadTimeMinutes = value })));

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.Rejected, result.Status);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteTypedPrediction_ReturnsRejected(double value)
    {
        var result = FastApiPredictionClient.Validate(SuccessPayload() with { WorkingLeadTimeMinutes = value });

        Assert.Equal(AiProviderStatus.Rejected, result.Status);
    }

    [Theory]
    [InlineData("{\"workingLeadTimeMinutes\":NaN,\"modelVersion\":\"xgb-v0.1\",\"featureSchemaVersion\":\"1\",\"trainingDatasetVersion\":\"synthetic-v1\"}")]
    [InlineData("{\"workingLeadTimeMinutes\":Infinity,\"modelVersion\":\"xgb-v0.1\",\"featureSchemaVersion\":\"1\",\"trainingDatasetVersion\":\"synthetic-v1\"}")]
    public async Task NonStandardNonFiniteJson_ReturnsInvalidResponse(string json)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var (client, _) = Create(new RecordingHandler(response));

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.InvalidResponse, result.Status);
    }

    [Theory]
    [InlineData("other", "1", "synthetic-v1")]
    [InlineData("xgb-v0.1", "2", "synthetic-v1")]
    [InlineData("xgb-v0.1", "1", "other")]
    public async Task VersionMismatch_ReturnsControlledStatus(string model, string schema, string dataset)
    {
        var payload = SuccessPayload() with
        {
            ModelVersion = model,
            FeatureSchemaVersion = schema,
            TrainingDatasetVersion = dataset
        };
        var (client, _) = Create(new RecordingHandler(Json(payload)));

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.VersionMismatch, result.Status);
    }

    [Theory]
    [InlineData(null, "1", "synthetic-v1")]
    [InlineData("xgb-v0.1", null, "synthetic-v1")]
    [InlineData("xgb-v0.1", "1", null)]
    public async Task MissingVersionMetadata_ReturnsInvalidResponse(string? model, string? schema, string? dataset)
    {
        var payload = SuccessPayload() with
        {
            ModelVersion = model,
            FeatureSchemaVersion = schema,
            TrainingDatasetVersion = dataset
        };
        var (client, _) = Create(new RecordingHandler(Json(payload)));

        var result = await client.GetPredictionAsync(Request());

        Assert.Equal(AiProviderStatus.InvalidResponse, result.Status);
    }

    [Fact]
    public async Task LogsNeverContainFeaturePayloadSentinels()
    {
        var (client, logs) = Create(new RecordingHandler(Json(SuccessPayload())));

        await client.GetPredictionAsync(Request());

        var serializedLogs = JsonSerializer.Serialize(logs.Requests);
        Assert.DoesNotContain("SENTINEL-PRODUCT-908", serializedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain("987654.321", serializedLogs, StringComparison.Ordinal);
    }

    private static (FastApiPredictionClient Client, RecordingLogWriter Logs) Create(
        HttpMessageHandler handler,
        int timeoutMs = 1000)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://ai.test/") };
        var logs = new RecordingLogWriter();
        return (new FastApiPredictionClient(
            httpClient,
            logs,
            new AiPredictionOptions { BaseUrl = "https://ai.test", TimeoutMs = timeoutMs }), logs);
    }

    private static AiPredictionRequest Request() => new(new AiFeaturePayload(
        1, "SENTINEL-PRODUCT-908", "CATEGORY-SENTINEL-908", 987654.321m,
        3, 1, 2m, null, 2, 75, null, null, null, null, null, null, null));

    private static FastApiPredictionResponse SuccessPayload() =>
        new(4320, "xgb-v0.1", "1", "synthetic-v1");

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json")
    };

    private sealed class RecordingLogWriter : IIntegrationLogWriter
    {
        public List<IntegrationLogRequest> Requests { get; } = [];

        public Task WriteAsync(IntegrationLogRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Body);

    private sealed class RecordingHandler(params object[] outcomes) : HttpMessageHandler
    {
        private readonly Queue<object> _outcomes = new(outcomes);
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri!, body));
            var outcome = _outcomes.Dequeue();
            if (outcome is Exception exception)
            {
                throw exception;
            }

            return (HttpResponseMessage)outcome;
        }
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        public CancellationToken ObservedToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ObservedToken = cancellationToken;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException();
        }
    }
}
