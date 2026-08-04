using System.Text.Json;
using App.Api.Configuration;
using App.Application.Contracts.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace App.Api.Tests.Configuration;

public class MvpAssumptionsConfigurationTests
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "mvp-assumptions.json");

    [Fact]
    public void ConfigurationFile_HasExactExpectedShapeAndValues()
    {
        Assert.True(File.Exists(ConfigPath));

        using var document = JsonDocument.Parse(File.ReadAllText(ConfigPath));
        var rootProperties = document.RootElement.EnumerateObject().ToArray();
        Assert.Single(rootProperties);
        Assert.Equal(MvpAssumptionsOptions.SectionName, rootProperties[0].Name);

        var groups = rootProperties[0].Value.EnumerateObject().ToArray();
        Assert.Equal(new[] { "workingCalendar", "procurement", "shipping" }, groups.Select(group => group.Name));
        AssertSingleProperty(groups[0].Value, "minutesPerDay", JsonValueKind.Number);
        AssertSingleProperty(groups[1].Value, "fallbackDurationMinutes", JsonValueKind.Number);
        AssertSingleProperty(groups[2].Value, "fallbackDurationMinutes", JsonValueKind.Null);
        Assert.Equal(480, groups[0].Value.GetProperty("minutesPerDay").GetInt64());
        Assert.Equal(960, groups[1].Value.GetProperty("fallbackDurationMinutes").GetInt64());
    }

    [Fact]
    public void ConfigurationFile_ContainsNoMachineCountOrErpReferenceFields()
    {
        var json = File.ReadAllText(ConfigPath);
        var forbiddenFields = new[]
        {
            "machineCount", "defaultMachineCount", "workCenterRef", "workCenterReference",
            "productReference", "orderReference", "supplierReference", "originReference",
            "destinationReference", "shippingProfileReference"
        };

        Assert.All(forbiddenFields, field => Assert.DoesNotContain(field, json, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OptionsTypes_DoNotExposeMachineCount()
    {
        var optionTypes = new[]
        {
            typeof(MvpAssumptionsOptions), typeof(WorkingCalendarAssumptionsOptions),
            typeof(ProcurementAssumptionsOptions), typeof(ShippingAssumptionsOptions)
        };

        Assert.All(optionTypes, type =>
            Assert.DoesNotContain(type.GetProperties(), property =>
                property.Name.Contains("MachineCount", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Configuration_BindsAndResolvesThroughIOptions()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddMvpAssumptions()
            .Build();
        var services = new ServiceCollection();
        services.AddMvpAssumptionsOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MvpAssumptionsOptions>>().Value;

        Assert.Equal(480, options.WorkingCalendar.MinutesPerDay);
        Assert.Equal(960, options.Procurement.FallbackDurationMinutes);
        Assert.Null(options.Shipping.FallbackDurationMinutes);
    }

    [Theory]
    [InlineData(1, 1, null, true)]
    [InlineData(1, 1, 1, true)]
    [InlineData(0, 1, null, false)]
    [InlineData(-1, 1, null, false)]
    [InlineData(1, 0, null, false)]
    [InlineData(1, -1, null, false)]
    [InlineData(1, 1, 0, false)]
    [InlineData(1, 1, -1, false)]
    public void OptionsValidation_EnforcesPositiveDurations(
        long minutesPerDay,
        long procurementFallback,
        int? shippingFallback,
        bool isValid)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{MvpAssumptionsOptions.SectionName}:workingCalendar:minutesPerDay"] = minutesPerDay.ToString(),
            [$"{MvpAssumptionsOptions.SectionName}:procurement:fallbackDurationMinutes"] = procurementFallback.ToString(),
            [$"{MvpAssumptionsOptions.SectionName}:shipping:fallbackDurationMinutes"] = shippingFallback?.ToString()
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddMvpAssumptionsOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var resolve = () => provider.GetRequiredService<IOptions<MvpAssumptionsOptions>>().Value;

        if (isValid)
        {
            _ = resolve();
        }
        else
        {
            Assert.Throws<OptionsValidationException>(resolve);
        }
    }

    private static void AssertSingleProperty(JsonElement element, string name, JsonValueKind valueKind)
    {
        var properties = element.EnumerateObject().ToArray();
        Assert.Single(properties);
        Assert.Equal(name, properties[0].Name);
        Assert.Equal(valueKind, properties[0].Value.ValueKind);
    }
}
