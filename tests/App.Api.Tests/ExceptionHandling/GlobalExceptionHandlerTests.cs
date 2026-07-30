using System.Text.Json;
using App.Api.ExceptionHandling;
using App.Api.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace App.Api.Tests.ExceptionHandling;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_Returns500WithSafeProblemDetailsBody()
    {
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException(
            "Connection string: Host=prod-db;Password=SuperSecret123 at App.Persistence.AppDbContext.OnConfiguring");

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails!.Status);
        Assert.Equal("An unexpected error occurred. Please try again later.", problemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_ResponseBody_DoesNotContainStackTraceOrRawExceptionMessage()
    {
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException(
            "Connection string: Host=prod-db;Password=SuperSecret123 at App.Persistence.AppDbContext.OnConfiguring");

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.DoesNotContain("SuperSecret123", body);
        Assert.DoesNotContain("Connection string", body);
        Assert.DoesNotContain(exception.Message, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain("StackTrace", body);
    }

    [Fact]
    public async Task TryHandleAsync_LogsTheTechnicalExceptionAtErrorLevel()
    {
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException("db failure detail");

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Exception == exception);
    }
}
