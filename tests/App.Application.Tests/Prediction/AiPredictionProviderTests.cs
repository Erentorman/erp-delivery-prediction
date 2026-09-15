using App.Application.Contracts.Prediction;
using App.Application.Prediction;
using App.Domain.Prediction;
using Moq;

namespace App.Application.Tests.Prediction;

public sealed class AiPredictionProviderTests
{
    [Fact]
    public async Task PredictAsync_UsesFeatureBuilderAndCarriesXgbV01Prerequisites()
    {
        var features = Features();
        var builder = new Mock<IAiFeatureBuilder>(MockBehavior.Strict);
        var client = new Mock<IAiPredictionClient>(MockBehavior.Strict);
        var context = Context();
        builder.Setup(x => x.Build(context)).Returns(features);
        client.Setup(x => x.GetPredictionAsync(
                It.Is<AiPredictionRequest>(request => request.Features == features),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiPredictionResult(AiProviderStatus.Success, 4320));
        var provider = new AiPredictionProvider(builder.Object, client.Object);

        var result = await provider.PredictAsync(context);

        Assert.Equal(AiProviderStatus.Success, result.Status);
        builder.VerifyAll();
        client.VerifyAll();
    }

    [Fact]
    public async Task PredictAsync_AcceptsDeferredNullableFeatures()
    {
        var features = Features();
        Assert.Null(features.MaximumSupplierLeadTimeDays);
        Assert.Null(features.WorkCenterLoadRatio);
        Assert.Null(features.ActiveWorkOrderCount);
        Assert.Null(features.ShiftCapacityMinutes);
        Assert.Null(features.HolidayCount);
        Assert.Null(features.PlannedDowntimeMinutes);
        Assert.Null(features.ShippingDurationMinutes);
        Assert.Null(features.RequestedDeliveryLeadMinutes);
        var builder = Mock.Of<IAiFeatureBuilder>(x => x.Build(It.IsAny<PredictionContext>()) == features);
        var client = new Mock<IAiPredictionClient>();
        client.Setup(x => x.GetPredictionAsync(It.IsAny<AiPredictionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiPredictionResult(AiProviderStatus.Success, 4320));

        var result = await new AiPredictionProvider(builder, client.Object).PredictAsync(Context());

        Assert.Equal(AiProviderStatus.Success, result.Status);
        client.Verify(x => x.GetPredictionAsync(It.IsAny<AiPredictionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", 1, 1)]
    [InlineData("P001", 0, 1)]
    [InlineData("P001", 1, -1)]
    public async Task PredictAsync_WhenRequiredFeatureIsUnavailable_ReturnsInsufficientFeatures(
        string productRef,
        int quantity,
        int bomItemCount)
    {
        var features = Features() with
        {
            ProductRef = productRef,
            Quantity = quantity,
            BomItemCount = bomItemCount
        };
        var builder = Mock.Of<IAiFeatureBuilder>(x => x.Build(It.IsAny<PredictionContext>()) == features);
        var client = new Mock<IAiPredictionClient>(MockBehavior.Strict);

        var result = await new AiPredictionProvider(builder, client.Object).PredictAsync(Context());

        Assert.Equal(AiProviderStatus.InsufficientFeatures, result.Status);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PredictAsync_PropagatesCancellationTokenToClient()
    {
        using var source = new CancellationTokenSource();
        var builder = Mock.Of<IAiFeatureBuilder>(x => x.Build(It.IsAny<PredictionContext>()) == Features());
        var client = new Mock<IAiPredictionClient>();
        client.Setup(x => x.GetPredictionAsync(It.IsAny<AiPredictionRequest>(), source.Token))
            .ReturnsAsync(new AiPredictionResult(AiProviderStatus.Success, 4320));

        await new AiPredictionProvider(builder, client.Object).PredictAsync(Context(), source.Token);

        client.VerifyAll();
    }

    private static AiFeaturePayload Features() => new(
        1, "P001", null, 2m, 3, 0, 0m, null, 1, 30,
        null, null, null, null, null, null, null);

    private static PredictionContext Context() => new(
        new OrderInput("O001", "P001", 2m, DateTimeOffset.UtcNow),
        new MaterialSnapshot([], [], [], []),
        new RoutingSnapshot([]),
        new CapacitySnapshot(),
        new CalendarSnapshot(),
        new ShippingSnapshot());
}
