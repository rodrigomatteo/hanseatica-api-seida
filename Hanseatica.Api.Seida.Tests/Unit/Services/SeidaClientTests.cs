namespace Hanseatica.Api.Seida.Tests.Unit.Services;

using Hanseatica.Api.Seida.Application.DTOs;
using Hanseatica.Api.Seida.Application.Exceptions;
using Hanseatica.Api.Seida.Application.Interfaces;
using Hanseatica.Api.Seida.Infrastructure.ExternalServices;
using Hanseatica.Api.Seida.Infrastructure.Services;
using Hanseatica.Api.Seida.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class SeidaClientTests
{
    private readonly Mock<ISeidaHttpClient> _mockHttpClient;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<SeidaClient>> _mockLogger;
    private readonly SeidaClient _client;

    public SeidaClientTests()
    {
        _mockHttpClient = new Mock<ISeidaHttpClient>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<SeidaClient>>();

        _client = new SeidaClient(
            _mockHttpClient.Object,
            _mockAuthService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SubmitFileAsync_WithValidData_ReturnsReceipt()
    {
        // Arrange
        var transactionNumber = "TXN123456";
        var fileContent = new byte[1024];
        var response = new SeidaReceiptResponse
        {
            anhoEnvio = "2024",
            documentoEmisor = "20100010136",
            fechaRecepcion = DateTime.UtcNow,
            hashDocumento = "HASH123",
            numeroOrden = "001",
            ticketEnvio = "0000000123",
            listaErrores = Array.Empty<SeidaErrorDetail>(),
            listaWarning = Array.Empty<SeidaErrorDetail>()
        };

        _mockHttpClient.Setup(x => x.SubmitFileAsync(
            It.IsAny<SubmitFileRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _client.SubmitFileAsync(transactionNumber, fileContent);

        // Assert
        result.Should().NotBeNull();
        result.TicketNumber.Should().Be("0000000123");
        result.SubmissionYear.Should().Be("2024");
    }

    [Fact]
    public async Task SubmitFileAsync_WhenHttpClientReturnsNull_ThrowsSeidaServiceException()
    {
        // Arrange
        var transactionNumber = "TXN123456";
        var fileContent = new byte[1024];

        _mockHttpClient.Setup(x => x.SubmitFileAsync(
            It.IsAny<SubmitFileRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeidaReceiptResponse)null!);

        // Act & Assert
        await Assert.ThrowsAsync<SeidaServiceException>(
            () => _client.SubmitFileAsync(transactionNumber, fileContent));
    }

    [Fact]
    public async Task SubmitFileAsync_WhenHttpClientThrowsHttpRequestException_ThrowsSeidaServiceException()
    {
        // Arrange
        var transactionNumber = "TXN123456";
        var fileContent = new byte[1024];

        _mockHttpClient.Setup(x => x.SubmitFileAsync(
            It.IsAny<SubmitFileRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SeidaServiceException>(
            () => _client.SubmitFileAsync(transactionNumber, fileContent));
        ex.Message.Should().Contain("Failed to communicate");
    }

    [Fact]
    public async Task SubmitFileAsync_WithErrorsInResponse_ReturnsMappedErrors()
    {
        // Arrange
        var transactionNumber = "TXN123456";
        var fileContent = new byte[1024];
        var response = new SeidaReceiptResponse
        {
            anhoEnvio = "2024",
            documentoEmisor = "20100010136",
            fechaRecepcion = DateTime.UtcNow,
            hashDocumento = "HASH123",
            numeroOrden = "001",
            ticketEnvio = "0000000123",
            listaErrores = new[]
            {
                new SeidaErrorDetail { codigo = "ERR001", descripcion = "Error 1", tipo = "Error" },
                new SeidaErrorDetail { codigo = "ERR002", descripcion = "Error 2", tipo = "Error" }
            },
            listaWarning = Array.Empty<SeidaErrorDetail>()
        };

        _mockHttpClient.Setup(x => x.SubmitFileAsync(
            It.IsAny<SubmitFileRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _client.SubmitFileAsync(transactionNumber, fileContent);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.First().Code.Should().Be("ERR001");
    }

    [Fact]
    public async Task QueryStatusAsync_WithValidData_ReturnsQueryResponse()
    {
        // Arrange
        var year = "2024";
        var ticketNumber = "0000000123";
        var response = new SeidaQueryResponse
        {
            descripcion = "Query completed",
            tipo = "ACEPTADO",
            datos = new byte[256]
        };

        _mockHttpClient.Setup(x => x.QueryStatusAsync(
            It.IsAny<QueryStatusRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _client.QueryStatusAsync(year, ticketNumber);

        // Assert
        result.Should().NotBeNull();
        result.StatusType.Should().Be("ACEPTADO");
        result.Description.Should().Be("Query completed");
    }

    [Fact]
    public async Task QueryStatusAsync_WhenHttpClientReturnsNull_ThrowsSeidaServiceException()
    {
        // Arrange
        var year = "2024";
        var ticketNumber = "0000000123";

        _mockHttpClient.Setup(x => x.QueryStatusAsync(
            It.IsAny<QueryStatusRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeidaQueryResponse)null!);

        // Act & Assert
        await Assert.ThrowsAsync<SeidaServiceException>(
            () => _client.QueryStatusAsync(year, ticketNumber));
    }

    [Fact]
    public async Task QueryStatusAsync_IncludesQueryXmlInRequest()
    {
        // Arrange
        var year = "2024";
        var ticketNumber = "0000000123";
        var expectedQueryXml = $"<consulta><tipo>1</tipo><parametros><numeroTicket>{ticketNumber}</numeroTicket><annoTicket>{year}</annoTicket></parametros></consulta>";
        var response = new SeidaQueryResponse { descripcion = "OK", tipo = "ACEPTADO", datos = Array.Empty<byte>() };

        _mockHttpClient.Setup(x => x.QueryStatusAsync(
            It.IsAny<QueryStatusRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response)
            .Callback<QueryStatusRequest, CancellationToken>((req, _) =>
            {
                req.QueryParameters.Should().Be(expectedQueryXml);
            });

        // Act
        await _client.QueryStatusAsync(year, ticketNumber);

        // Assert - verified in Callback
    }

    [Fact]
    public async Task SubmitFileAsync_PassesCorrectTransactionNumberToHttpClient()
    {
        // Arrange
        var transactionNumber = "UNIQUE_TXN_12345";
        var fileContent = new byte[1024];
        var response = new SeidaReceiptResponse
        {
            anhoEnvio = "2024",
            documentoEmisor = "20100010136",
            fechaRecepcion = DateTime.UtcNow,
            hashDocumento = "HASH123",
            numeroOrden = "001",
            ticketEnvio = "0000000123",
            listaErrores = Array.Empty<SeidaErrorDetail>(),
            listaWarning = Array.Empty<SeidaErrorDetail>()
        };

        _mockHttpClient.Setup(x => x.SubmitFileAsync(
            It.IsAny<SubmitFileRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response)
            .Callback<SubmitFileRequest, CancellationToken>((req, _) =>
            {
                req.TransactionNumber.Should().Be(transactionNumber);
                req.FileContent.Should().Equal(fileContent);
            });

        // Act
        await _client.SubmitFileAsync(transactionNumber, fileContent);

        // Assert - verified in Callback
    }
}
