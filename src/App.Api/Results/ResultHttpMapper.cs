using App.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Results;

public static class ResultHttpMapper
{
    public static int MapToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, "Unmapped error type.")
    };

    public static ProblemDetails ToProblemDetails(Error error)
    {
        var statusCode = MapToStatusCode(error.Type);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Type.ToString(),
            Detail = error.Message
        };
        problemDetails.Extensions["errorCode"] = error.Code;

        return problemDetails;
    }
}
