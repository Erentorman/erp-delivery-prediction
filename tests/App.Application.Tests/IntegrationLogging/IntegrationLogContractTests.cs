using App.Application.IntegrationLogging;

namespace App.Application.Tests.IntegrationLogging;

public class IntegrationLogContractTests
{
    [Theory]
    [InlineData(IntegrationType.Erp)]
    [InlineData(IntegrationType.Ai)]
    public void Request_RepresentsAllIntegrationMetadata(IntegrationType integrationType)
    {
        var request = new IntegrationLogRequest(
            integrationType,
            "GetOrder",
            "orders",
            true,
            200,
            125,
            "Completed");

        Assert.Equal(integrationType, request.IntegrationType);
        Assert.Equal("GetOrder", request.Operation);
        Assert.Equal("orders", request.ExternalResource);
        Assert.True(request.IsSuccess);
        Assert.Equal(200, request.StatusCode);
        Assert.Equal(125, request.DurationMs);
        Assert.Equal("Completed", request.Message);
    }

    [Fact]
    public void Request_AllowsNullableStatusCodeAndMessage()
    {
        var request = new IntegrationLogRequest(
            IntegrationType.Erp,
            "Synchronize",
            "inventory",
            false,
            null,
            0,
            null);

        Assert.Null(request.StatusCode);
        Assert.Null(request.Message);
    }

    [Fact]
    public void Contract_DoesNotExposePayloadOrSecretFields()
    {
        var forbiddenTerms = new[]
        {
            "Payload", "RequestBody", "ResponseBody", "Password",
            "Token", "Authorization", "ConnectionString"
        };
        var propertyNames = typeof(IntegrationLogRequest)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(
            propertyNames,
            name => forbiddenTerms.Any(term =>
                name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
