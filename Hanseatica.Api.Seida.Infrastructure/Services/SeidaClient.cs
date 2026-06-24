namespace Hanseatica.Api.Seida.Infrastructure.Services;

using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Implementation of SEIDA client
/// </summary>
public class SeidaClient : ISeidaClient
{
    private readonly ISeidaHttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly SeidaClientOptions _options;
    private readonly ILogger<SeidaClient> _logger;

    public SeidaClient(
        ISeidaHttpClient httpClient,
        IConfiguration configuration,
        IOptions<SeidaClientOptions> options,
        ILogger<SeidaClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReceiptDto> SubmitFileAsync(string transactionNumber, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting file to SEIDA service with transaction: {TransactionNumber}", transactionNumber);

            var numeroRuc = _configuration["Seida:NumeroRUC"]
                ?? throw new SeidaServiceException("SEIDA NumeroRUC is not configured");
            var usuarioSOL = _configuration["Seida:UsuarioSOL"]
                ?? throw new SeidaServiceException("SEIDA UsuarioSOL is not configured");
            var solPassword = _configuration["Seida:SolPassword"]
                ?? throw new SeidaServiceException("SEIDA SolPassword is not configured");

            // Validate RUC number
            try
            {
                RucNumberValidator.Validate(numeroRuc);
            }
            catch (ArgumentException ex)
            {
                throw new SeidaServiceException($"Invalid RUC number: {ex.Message}", ex);
            }

            // Validate and normalize SOL user
            string normalizedSolUser;
            try
            {
                normalizedSolUser = SolUserValidator.ValidateAndNormalize(usuarioSOL);
            }
            catch (ArgumentException ex)
            {
                throw new SeidaServiceException($"Invalid SOL user: {ex.Message}", ex);
            }

            // Validate SOL password
            try
            {
                SolPasswordValidator.Validate(solPassword);
            }
            catch (ArgumentException ex)
            {
                throw new SeidaServiceException($"Invalid SOL password: {ex.Message}", ex);
            }

            ValidateTransactionNumber(transactionNumber, numeroRuc);

            var compositeUsername = BuildCompositeUsername(numeroRuc, normalizedSolUser);

            var validationErrors = CompositeUsernameValidator.ValidateComponents(
                numeroRuc, normalizedSolUser, _options.MacAddress, _options.IpAddress);
            if (validationErrors.Any())
                throw new SeidaServiceException(
                    $"Invalid credential components: {string.Join("; ", validationErrors)}");

            var request = new SubmitFileRequest
            {
                Username = compositeUsername,
                Password = solPassword,
                TransactionNumber = transactionNumber,
                FileContent = Convert.ToBase64String(fileContent)
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

            var numeroRuc = _configuration["Seida:NumeroRUC"]
                ?? throw new SeidaServiceException("SEIDA NumeroRUC is not configured");
            var usuarioSOL = _configuration["Seida:UsuarioSOL"]
                ?? throw new SeidaServiceException("SEIDA UsuarioSOL is not configured");
            var solPassword = _configuration["Seida:SolPassword"]
                ?? throw new SeidaServiceException("SEIDA SolPassword is not configured");

            // Validate RUC number
            try
            {
                RucNumberValidator.Validate(numeroRuc);
            }
            catch (ArgumentException ex)
            {
                throw new SeidaServiceException($"Invalid RUC number: {ex.Message}", ex);
            }

            // Validate and normalize SOL user
            string normalizedSolUser;
            try
            {
                normalizedSolUser = SolUserValidator.ValidateAndNormalize(usuarioSOL);
            }
            catch (ArgumentException ex)
            {
                throw new SeidaServiceException($"Invalid SOL user: {ex.Message}", ex);
            }

            // Validate SOL password
            try
            {
                SolPasswordValidator.Validate(solPassword);
            }
            catch (ArgumentException ex)
            {
                throw new SeidaServiceException($"Invalid SOL password: {ex.Message}", ex);
            }

            var compositeUsername = BuildCompositeUsername(numeroRuc, normalizedSolUser);

            var validationErrors = CompositeUsernameValidator.ValidateComponents(
                numeroRuc, normalizedSolUser, _options.MacAddress, _options.IpAddress);
            if (validationErrors.Any())
                throw new SeidaServiceException(
                    $"Invalid credential components: {string.Join("; ", validationErrors)}");

            var queryXml = $"<consulta><tipo>1</tipo><parametros><numeroTicket>{ticketNumber.Trim()}</numeroTicket><annoTicket>{year}</annoTicket></parametros></consulta>";

            var request = new QueryStatusRequest
            {
                Username = compositeUsername,
                Password = solPassword,
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

    private string BuildCompositeUsername(string numeroRuc, string usuarioSOL)
    {
        return $"{numeroRuc}{usuarioSOL}|{_options.MacAddress}|{_options.IpAddress}|2";
    }

    private static void ValidateTransactionNumber(string transactionNumber, string numeroRuc)
    {
        if (string.IsNullOrWhiteSpace(transactionNumber))
            throw new SeidaServiceException("Transaction number cannot be empty");

        var parts = transactionNumber.Split('-');
        if (parts.Length != 4)
            throw new SeidaServiceException(
                $"Invalid transaction number format. Expected: {{RUC}}-{{YYYYMMDD}}-{{HHMMSS}}-{{SEQUENCE}}, got: {transactionNumber}");

        if (parts[0] != numeroRuc)
            throw new SeidaServiceException(
                $"Transaction number RUC '{parts[0]}' does not match configured RUC '{numeroRuc}'");

        if (!IsValidDate(parts[1]))
            throw new SeidaServiceException(
                $"Invalid date format in transaction number. Expected YYYYMMDD, got: {parts[1]}");

        if (!IsValidTime(parts[2]))
            throw new SeidaServiceException(
                $"Invalid time format in transaction number. Expected HHMMSS, got: {parts[2]}");

        if (!int.TryParse(parts[3], out _))
            throw new SeidaServiceException(
                $"Invalid sequence number in transaction number. Expected numeric value, got: {parts[3]}");
    }

    private static bool IsValidDate(string dateStr)
    {
        if (dateStr.Length != 8 || !dateStr.All(char.IsDigit))
            return false;
        return DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _);
    }

    private static bool IsValidTime(string timeStr)
    {
        if (timeStr.Length != 6 || !timeStr.All(char.IsDigit))
            return false;
        return DateTime.TryParseExact(timeStr, "HHmmss", null, System.Globalization.DateTimeStyles.None, out _);
    }

    /// <summary>
    /// Generates a transaction number in SUNAT SEIDA format: {RUC}-{YYYYMMDD}-{HHMMSS}-{SEQUENCE}
    /// </summary>
    public static string GenerateTransactionNumber(string numeroRuc, int sequence = 1)
    {
        var now = DateTime.Now;
        var date = now.ToString("yyyyMMdd");
        var time = now.ToString("HHmmss");
        return $"{numeroRuc}-{date}-{time}-{sequence:D3}";
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
            ResponseData = response.datos,
            ErrorCode = response.errorCode,
            ErrorMessage = response.errorMessage
        };
    }
}
