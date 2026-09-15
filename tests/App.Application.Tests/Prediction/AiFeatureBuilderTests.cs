using System.Text.Json;
using App.Application.Contracts.Prediction;
using App.Application.Prediction;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction;

public sealed class AiFeatureBuilderTests
{
    private readonly AiFeatureBuilder _builder = new();

    [Fact]
    public void Build_MapsEvidenceBackedRawFeaturesAndPreservesUnavailableValuesAsNull()
    {
        var context = CreateContext();

        var payload = _builder.Build(context);

        Assert.Equal(1, payload.FeatureSchemaVersion);
        Assert.Equal("P001", payload.ProductRef);
        Assert.Equal("CATEGORY-A", payload.ProductCategory);
        Assert.Equal(3m, payload.Quantity);
        Assert.Equal(2, payload.BomItemCount);
        Assert.Equal(2, payload.MissingMaterialCount);
        Assert.Equal(8m, payload.TotalMissingQuantity);
        Assert.Equal(2, payload.OperationCount);
        Assert.Equal(75, payload.TotalStandardOperationMinutes);
        Assert.Null(payload.MaximumSupplierLeadTimeDays);
        Assert.Null(payload.WorkCenterLoadRatio);
        Assert.Null(payload.ActiveWorkOrderCount);
        Assert.Null(payload.ShiftCapacityMinutes);
        Assert.Null(payload.HolidayCount);
        Assert.Null(payload.PlannedDowntimeMinutes);
        Assert.Null(payload.ShippingDurationMinutes);
        Assert.Null(payload.RequestedDeliveryLeadMinutes);
    }

    [Fact]
    public void Build_ProducesCurrentXgbV01RequiredRuntimeFeaturesTogether()
    {
        var payload = _builder.Build(CreateContext());

        Assert.Equal("P001", payload.ProductRef);
        Assert.Equal(3m, payload.Quantity);
        Assert.Equal(2, payload.BomItemCount);
    }

    [Fact]
    public void Build_FiltersBomToOrderedProductAndTreatsAbsentStockAsZero()
    {
        var context = CreateContext();

        var payload = _builder.Build(context);

        Assert.Equal(2, payload.BomItemCount);
        Assert.Equal(2, payload.MissingMaterialCount);
        Assert.Equal(8m, payload.TotalMissingQuantity);
    }

    [Fact]
    public void Build_DoesNotEmitNegativeShortageWhenStockCoversRequirement()
    {
        var context = CreateContext() with
        {
            MaterialSnapshot = new MaterialSnapshot(
                [new MaterialProduct("P001", "pcs")],
                [new MaterialBomItem("P001", "C001", 2m)],
                [new MaterialStock("C001", 6m)],
                [])
        };

        var payload = _builder.Build(context);

        Assert.Equal(0, payload.MissingMaterialCount);
        Assert.Equal(0m, payload.TotalMissingQuantity);
    }

    [Fact]
    public void Build_UsesRawRoutingAndIgnoresPipelineEnrichedOperations()
    {
        var context = CreateContext();
        context.AddOperation(new Operation("PIPELINE-ONLY", 9999));

        var payload = _builder.Build(context);

        Assert.Equal(2, payload.OperationCount);
        Assert.Equal(75, payload.TotalStandardOperationMinutes);
    }

    [Fact]
    public void Build_WithMissingPlanningClassification_KeepsCategoryNull()
    {
        var context = CreateContext() with
        {
            MaterialSnapshot = CreateContext().MaterialSnapshot with
            {
                Products = [new MaterialProduct("P001", "pcs")]
            }
        };

        Assert.Null(_builder.Build(context).ProductCategory);
    }

    [Fact]
    public void Build_Repeatedly_ReturnsEqualPayloadAndStableJsonPropertyOrder()
    {
        var context = CreateContext();

        var first = _builder.Build(context);
        var second = _builder.Build(context);
        var propertyNames = JsonDocument.Parse(JsonSerializer.Serialize(
                first,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
            .RootElement.EnumerateObject().Select(property => property.Name).ToArray();

        Assert.Equal(first, second);
        Assert.Equal(
            [
                "featureSchemaVersion", "productRef", "productCategory", "quantity",
                "bomItemCount", "missingMaterialCount", "totalMissingQuantity",
                "maximumSupplierLeadTimeDays", "operationCount", "totalStandardOperationMinutes",
                "workCenterLoadRatio", "activeWorkOrderCount", "shiftCapacityMinutes",
                "holidayCount", "plannedDowntimeMinutes", "shippingDurationMinutes",
                "requestedDeliveryLeadMinutes"
            ],
            propertyNames);
    }

    [Fact]
    public void Payload_ContainsOnlyTheVersionAndSadFeatureContract()
    {
        var propertyNames = typeof(AiFeaturePayload).GetProperties()
            .Select(property => property.Name).ToArray();

        Assert.Equal(17, propertyNames.Length);
        Assert.DoesNotContain(propertyNames, name =>
            name.Contains("Customer", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("RuleBased", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("CriticalPath", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuilderContract_DependsOnlyOnPredictionContextAndPayload()
    {
        var buildMethod = typeof(IAiFeatureBuilder).GetMethod(nameof(IAiFeatureBuilder.Build));

        Assert.NotNull(buildMethod);
        Assert.Equal(typeof(AiFeaturePayload), buildMethod.ReturnType);
        Assert.Equal([typeof(PredictionContext)], buildMethod.GetParameters().Select(p => p.ParameterType));
        Assert.Empty(typeof(AiFeatureBuilder).GetConstructors().Single().GetParameters());
    }

    private static PredictionContext CreateContext()
    {
        return new PredictionContext(
            new OrderInput("O001", "P001", 3m, new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero)),
            new MaterialSnapshot(
                [new MaterialProduct("P001", "pcs", "CATEGORY-A")],
                [
                    new MaterialBomItem("P001", "C001", 2m),
                    new MaterialBomItem("P001", "C002", 1m),
                    new MaterialBomItem("OTHER", "C003", 100m)
                ],
                [new MaterialStock("C001", 1m)],
                []),
            new RoutingSnapshot(
                [
                    new RoutingOperation("OP10", 10, "WC1", 30, []),
                    new RoutingOperation("OP20", 20, "WC2", 45, ["OP10"])
                ]),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            new ShippingSnapshot());
    }
}
