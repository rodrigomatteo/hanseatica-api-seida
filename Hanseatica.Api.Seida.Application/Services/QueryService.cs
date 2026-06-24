namespace Hanseatica.Api.Seida.Application.Services;

using DTOs;
using Exceptions;
using Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for handling status queries
/// </summary>
public class QueryService : IQueryService
{
    private readonly IFileService _fileService;
    private readonly IAuthenticationService _authService;
    private readonly ISeidaClient _seidaClient;
    private readonly ILogger<QueryService> _logger;

    public QueryService(
        IFileService fileService,
        IAuthenticationService authService,
        ISeidaClient seidaClient,
        ILogger<QueryService> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _seidaClient = seidaClient ?? throw new ArgumentNullException(nameof(seidaClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QueryResponseDto> QueryStatusAsync(QueryRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ValidationException("request", "Query request cannot be null");

        if (string.IsNullOrWhiteSpace(request.Year))
            throw new ValidationException("Year", "Year is required");

        if (string.IsNullOrWhiteSpace(request.TicketNumber))
            throw new ValidationException("TicketNumber", "Ticket number is required");

        return await QueryStatusAsync(request.Year, request.TicketNumber, cancellationToken);
    }

    public async Task<QueryResponseDto> QueryStatusAsync(string year, string ticketNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying status for ticket: {TicketNumber}, year: {Year}", ticketNumber, year);

            //var credentials = _authService.GetCredentials();
            //if (!credentials.IsValid)
            //    throw new SeidaServiceException("Invalid authentication credentials");

            var response = await _seidaClient.QueryStatusAsync(year, ticketNumber, cancellationToken);

            _logger.LogInformation(
                "Query completed successfully for ticket: {TicketNumber}, status: {Status}",
                ticketNumber,
                response?.StatusType);

            return response ?? throw new SeidaServiceException("No response returned from SEIDA service");
        }
        catch (SeidaServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying status for ticket: {TicketNumber}", ticketNumber);
            throw new SeidaServiceException(
                $"Failed to query status: {ex.Message}",
                ex);
        }
    }
}
