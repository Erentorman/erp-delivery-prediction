using App.Api.Results;
using App.Application.Prediction;
using App.Application.Contracts.Prediction;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class PredictionsController : ControllerBase
{
    private readonly IPredictionCalculationService _predictionService;
    private readonly IWhatIfPredictionCalculationService _whatIfPredictionService;
    private readonly IPredictionRepository _predictionRepository;

    public PredictionsController(
        IPredictionCalculationService predictionService,
        IWhatIfPredictionCalculationService whatIfPredictionService,
        IPredictionRepository predictionRepository)
    {
        _predictionService = predictionService ?? throw new ArgumentNullException(nameof(predictionService));
        _whatIfPredictionService = whatIfPredictionService ?? throw new ArgumentNullException(nameof(whatIfPredictionService));
        _predictionRepository = predictionRepository ?? throw new ArgumentNullException(nameof(predictionRepository));
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculatePredictionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderReference))
        {
            return BadRequest("Order reference cannot be empty.");
        }

        var result = await _predictionService.CalculateAsync(request.OrderReference, cancellationToken);

        if (!result.IsSuccess)
        {
            var problemDetails = ResultHttpMapper.ToProblemDetails(result.Error!);
            return new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
        }

        return Ok(result.Value);
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate(
        [FromBody] WhatIfPredictionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _whatIfPredictionService.CalculateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            var problemDetails = ResultHttpMapper.ToProblemDetails(result.Error!);
            return new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
        }

        return Ok(result.Value);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<PredictionHistoryListItem>>> GetHistory(
        [FromQuery] string? orderReference,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var items = await _predictionRepository.GetHistoryAsync(orderReference, page, pageSize, cancellationToken);
        return Ok(items);
    }

    [HttpGet("history/{id:long}")]
    public async Task<ActionResult<PredictionHistoryDetail>> GetHistoryById(long id, CancellationToken cancellationToken)
    {
        var item = await _predictionRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }
}

public sealed record CalculatePredictionRequest(string OrderReference);
