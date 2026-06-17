namespace Hanseatica.Api.Seida.Infrastructure.Services;

using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using ExternalServices;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of SEIDA client
/// </summary>
public class SeidaClient : ISeidaClient
{
    private readonly ISeidaHttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<SeidaClient> _logger;

    public SeidaClient(
        ISeidaHttpClient httpClient,
        IAuthenticationService authService,
        ILogger<SeidaClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReceiptDto> SubmitFileAsync(string transactionNumber, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting file to SEIDA service with transaction: {TransactionNumber}", transactionNumber);

            var credentials = _authService.GetCredentials();
            if (!credentials.IsValid)
                throw new SeidaServiceException("Invalid authentication credentials");

            var request = new SubmitFileRequest
            {
                Username = credentials.Username,
                EncryptedPassword = _authService.EncryptCredentials(credentials.Username, credentials.Password),
                TransactionNumber = transactionNumber,
                FileContent = fileContent
            };

            var response = await _httpClient.SubmitFileAsync(request, cancellationToken);

            if (response == null)
                throw new SeidaServiceException("No response received from SEIDA service");

            var receipt = MapToReceiptDto(response);

            _logger.LogInformation(
                "File submitted successfully. Ticket: {TicketNumber}, Errors: {ErrorCount}",
                receipt.TicketNumber,
                receipt.Errors.Count);

            return receipt;
        }
        catch (SeidaServiceException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error submitting file to SEIDA");
            throw new SeidaServiceException("Failed to communicate with SEIDA service", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting file to SEIDA");
            throw new SeidaServiceException("Failed to submit file to SEIDA service", ex);
        }
    }

    public async Task<QueryResponseDto> QueryStatusAsync(string year, string ticketNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying SEIDA service. Year: {Year}, Ticket: {TicketNumber}", year, ticketNumber);

            var credentials = _authService.GetCredentials();
            if (!credentials.IsValid)
                throw new SeidaServiceException("Invalid authentication credentials");

            var queryXml = $"<consulta><tipo>1</tipo><parametros><numeroTicket>{ticketNumber.Trim()}</numeroTicket><annoTicket>{year}</annoTicket></parametros></consulta>";

            var request = new QueryStatusRequest
            {
                Username = credentials.Username,
                EncryptedPassword = _authService.EncryptCredentials(credentials.Username, credentials.Password),
                QueryParameters = queryXml
            };

            var response = await _httpClient.QueryStatusAsync(request, cancellationToken);

            if (response == null)
                throw new SeidaServiceException("No response received from SEIDA service");

            var queryResponse = MapToQueryResponseDto(response);

            _logger.LogInformation(
                "Query completed. Status: {Status}, Description: {Description}",
                queryResponse.StatusType,
                queryResponse.Description);

            return queryResponse;
        }
        catch (SeidaServiceException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error querying SEIDA service");
            throw new SeidaServiceException("Failed to communicate with SEIDA service", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying SEIDA service");
            throw new SeidaServiceException("Failed to query SEIDA service", ex);
        }
    }

    private ReceiptDto MapToReceiptDto(SeidaReceiptResponse response)
    {
        return new ReceiptDto
        {
            SubmissionYear = response.anhoEnvio,
            IssuerDocument = response.documentoEmisor,
            ReceptionDate = response.fechaRecepcion,
            DocumentHash = response.hashDocumento,
            OrderNumber = response.numeroOrden,
            TicketNumber = response.ticketEnvio,
            Errors = response.listaErrores?.Select(e => new ErrorDetailDto
            {
                Code = e.codigo,
                Description = e.descripcion,
                Type = e.tipo
            }).ToList() ?? new(),
            Warnings = response.listaWarning?.Select(w => new ErrorDetailDto
            {
                Code = w.codigo,
                Description = w.descripcion,
                Type = w.tipo
            }).ToList() ?? new()
        };
    }

    private QueryResponseDto MapToQueryResponseDto(SeidaQueryResponse response)
    {
        return new QueryResponseDto
        {
            Description = response.descripcion,
            StatusType = response.tipo,
            ResponseData = response.datos
        };
    }
}
