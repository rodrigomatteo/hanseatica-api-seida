namespace Hanseatica.Api.Seida.Application.Services;

using DTOs;
using Exceptions;
using Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for handling file submissions to SEIDA
/// </summary>
public class SubmissionService : ISubmissionService
{
    private readonly IFileService _fileService;
    private readonly IAuthenticationService _authService;
    private readonly ISeidaClient _seidaClient;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        IFileService fileService,
        IAuthenticationService authService,
        ISeidaClient seidaClient,
        ILogger<SubmissionService> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _seidaClient = seidaClient ?? throw new ArgumentNullException(nameof(seidaClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReceiptDto> SubmitFileAsync(SubmissionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ValidationException("request", "Submission request cannot be null");

        if (string.IsNullOrWhiteSpace(request.TransactionNumber))
            throw new ValidationException("TransactionNumber", "Transaction number is required");

        if (request.FileContent == null || request.FileContent.Length == 0)
            throw new ValidationException("FileContent", "File content is required");

        return await SubmitFileAsync(request.TransactionNumber, request.FileContent, cancellationToken);
    }

    public async Task<ReceiptDto> SubmitFileAsync(string transactionNumber, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting file with transaction number: {TransactionNumber}", transactionNumber);

            var credentials = _authService.GetCredentials();
            if (!credentials.IsValid)
                throw new SeidaServiceException("Invalid authentication credentials");

            var receipt = await _seidaClient.SubmitFileAsync(transactionNumber, fileContent, cancellationToken);

            _logger.LogInformation(
                "File submitted successfully. Transaction: {TransactionNumber}, Ticket: {Ticket}",
                transactionNumber,
                receipt?.TicketNumber);

            return receipt ?? throw new SeidaServiceException("No receipt returned from SEIDA service");
        }
        catch (SeidaServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting file with transaction number: {TransactionNumber}", transactionNumber);
            throw new SeidaServiceException(
                $"Failed to submit file: {ex.Message}",
                ex);
        }
    }
}
