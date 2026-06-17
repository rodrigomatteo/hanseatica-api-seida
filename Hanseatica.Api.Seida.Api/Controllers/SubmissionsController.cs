namespace Hanseatica.Api.Seida.Api.Controllers;

using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for file submission operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly IFileService _fileService;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(
        ISubmissionService submissionService,
        IFileService fileService,
        ILogger<SubmissionsController> logger)
    {
        _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submit a file to SEIDA service
    /// </summary>
    /// <param name="request">Submission request with file content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Receipt with ticket number and validation results</returns>
    /// <response code="200">File submitted successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Server error</response>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(ReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitFile(
        [FromBody] SubmissionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request == null || request.FileContent == null || request.FileContent.Length == 0)
            {
                _logger.LogWarning("Invalid submission request received");
                return BadRequest(new { message = "File content is required" });
            }

            var receipt = await _submissionService.SubmitFileAsync(request, cancellationToken);
            return Ok(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitFile");
            return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
        }
    }

    /// <summary>
    /// Submit a file with a custom transaction number
    /// </summary>
    /// <param name="transactionNumber">Custom transaction number</param>
    /// <param name="file">File to submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Receipt with ticket number and validation results</returns>
    /// <response code="200">File submitted successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Server error</response>
    [HttpPost("submit-with-transaction")]
    [ProducesResponseType(typeof(ReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitFileWithTransaction(
        [FromQuery] string transactionNumber,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transactionNumber))
            {
                return BadRequest(new { message = "Transaction number is required" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File is required" });
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream, cancellationToken);
                var fileContent = stream.ToArray();

                var request = new SubmissionRequestDto
                {
                    TransactionNumber = transactionNumber,
                    FileContent = fileContent,
                    FileName = file.FileName
                };

                var receipt = await _submissionService.SubmitFileAsync(request, cancellationToken);
                return Ok(receipt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitFileWithTransaction");
            return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate a unique transaction number
    /// </summary>
    /// <returns>Generated transaction number</returns>
    [HttpGet("generate-transaction-id")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GenerateTransactionId()
    {
        var transactionId = _fileService.GenerateTransactionId();
        return Ok(new { transactionId });
    }
}
