namespace Hanseatica.Api.Seida.Api.Controllers;

using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for query operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class QueriesController : ControllerBase
{
    private readonly IQueryService _queryService;
    private readonly ILogger<QueriesController> _logger;

    public QueriesController(
        IQueryService queryService,
        ILogger<QueriesController> logger)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Query the status of a submission by ticket number
    /// </summary>
    /// <param name="request">Query request with year and ticket number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query response with status information</returns>
    /// <response code="200">Query completed successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Server error</response>
    [HttpPost("query-status")]
    [ProducesResponseType(typeof(QueryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QueryStatus(
        [FromBody] QueryRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Year) || string.IsNullOrWhiteSpace(request.TicketNumber))
            {
                _logger.LogWarning("Invalid query request received");
                return BadRequest(new { message = "Year and ticket number are required" });
            }

            var response = await _queryService.QueryStatusAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in QueryStatus");
            return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
        }
    }

    /// <summary>
    /// Query the status using URL parameters
    /// </summary>
    /// <param name="year">Submission year</param>
    /// <param name="ticketNumber">Ticket number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query response with status information</returns>
    /// <response code="200">Query completed successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Server error</response>
    [HttpGet("query-status")]
    [ProducesResponseType(typeof(QueryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QueryStatusByParams(
        [FromQuery] string year,
        [FromQuery] string ticketNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(year) || string.IsNullOrWhiteSpace(ticketNumber))
            {
                _logger.LogWarning("Invalid query parameters received");
                return BadRequest(new { message = "Year and ticket number are required" });
            }

            var request = new QueryRequestDto
            {
                Year = year,
                TicketNumber = ticketNumber
            };

            var response = await _queryService.QueryStatusAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in QueryStatusByParams");
            return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
        }
    }
}
