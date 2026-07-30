using App.Api.Results;
using App.Application.Common;
using Microsoft.AspNetCore.Http;

namespace App.Api.Tests.Results;

public class ResultHttpMapperTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    public void MapToStatusCode_ReturnsExpectedHttpStatusCode(ErrorType errorType, int expectedStatusCode)
    {
        var statusCode = ResultHttpMapper.MapToStatusCode(errorType);

        Assert.Equal(expectedStatusCode, statusCode);
    }

    [Fact]
    public void MapToStatusCode_UnmappedValue_ThrowsArgumentOutOfRangeException()
    {
        var invalidErrorType = (ErrorType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => ResultHttpMapper.MapToStatusCode(invalidErrorType));
    }

    [Fact]
    public void ToProblemDetails_CarriesSafeMessageAndErrorCode()
    {
        var error = new Error("NotFound.Order", "The requested order was not found.", ErrorType.NotFound);

        var problemDetails = ResultHttpMapper.ToProblemDetails(error);

        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("The requested order was not found.", problemDetails.Detail);
        Assert.Equal("NotFound.Order", problemDetails.Extensions["errorCode"]);
    }
}
