using App.Application.Common;

namespace App.Application.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ReturnsResultWithIsSuccessTrueAndNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ReturnsResultWithIsSuccessFalseAndCarriesError()
    {
        var error = new Error("NotFound.Order", "The requested order was not found.", ErrorType.NotFound);

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GenericSuccess_CarriesTheProvidedValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_CarriesErrorCodeAndSafeMessage()
    {
        var error = new Error("Validation.Required", "The field is required.", ErrorType.Validation);

        var result = Result<int>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error!.Code);
        Assert.Equal("The field is required.", result.Error.Message);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void GenericFailure_AccessingValueThrows()
    {
        var result = Result<int>.Failure(new Error("Conflict.Duplicate", "Already exists.", ErrorType.Conflict));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
