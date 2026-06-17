namespace Hanseatica.Api.Seida.Tests.Unit.Services;

using Hanseatica.Api.Seida.Application.DTOs;
using Hanseatica.Api.Seida.Application.Exceptions;
using Hanseatica.Api.Seida.Application.Interfaces;
using Hanseatica.Api.Seida.Application.Services;
using Hanseatica.Api.Seida.Domain.ValueObjects;
using Hanseatica.Api.Seida.Tests.Fixtures;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

public class QueryServiceTests
{
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ISeidaClient> _mockSeidaClient;
    private readonly Mock<ILogger<QueryService>> _mockLogger;
    private readonly QueryService _service;

    public QueryServiceTests()
    {
        _mockFileService = new Mock<IFileService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockSeidaClient = new Mock<ISeidaClient>();
        _mockLogger = new Mock<ILogger<QueryService>>();

        _service = new QueryService(
            _mockFileService.Object,
            _mockAuthService.Object,
            _mockSeidaClient.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task QueryStatusAsync_WithValidRequest_ReturnsQueryResponse()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        var expectedResponse = TestDataFixtures.CreateValidQueryResponse();
        var credentials = new SecurityCredentials("testuser", "testpass");

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(credentials);
        _mockSeidaClient.Setup(x => x.QueryStatusAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.QueryStatusAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.StatusType.Should().Be(expectedResponse.StatusType);
        result.Description.Should().Be(expectedResponse.Description);
        _mockSeidaClient.Verify(
            x => x.QueryStatusAsync(request.Year, request.TicketNumber, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task QueryStatusAsync_WithNullRequest_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.QueryStatusAsync((QueryRequestDto)null!));
    }

    [Fact]
    public async Task QueryStatusAsync_WithMissingYear_ThrowsValidationException()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        request.Year = "";

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.QueryStatusAsync(request));
    }

    [Fact]
    public async Task QueryStatusAsync_WithMissingTicketNumber_ThrowsValidationException()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        request.TicketNumber = "";

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.QueryStatusAsync(request));
    }

    [Fact]
    public async Task QueryStatusAsync_WithInvalidCredentials_ThrowsSeidaServiceException()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        var invalidCredentials = new SecurityCredentials();

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(invalidCredentials);

        // Act & Assert
        await Assert.ThrowsAsync<SeidaServiceException>(
            () => _service.QueryStatusAsync(request));
    }

    [Fact]
    public async Task QueryStatusAsync_WhenSeidaClientThrows_ThrowsSeidaServiceException()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        var credentials = new SecurityCredentials("testuser", "testpass");

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(credentials);
        _mockSeidaClient.Setup(x => x.QueryStatusAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SeidaServiceException>(
            () => _service.QueryStatusAsync(request));
        ex.Message.Should().Contain("Failed to query status");
    }

    [Fact]
    public async Task QueryStatusAsync_WithStringOverload_ReturnsQueryResponse()
    {
        // Arrange
        var year = DateTime.UtcNow.Year.ToString();
        var ticketNumber = "0000000123";
        var expectedResponse = TestDataFixtures.CreateValidQueryResponse();
        var credentials = new SecurityCredentials("testuser", "testpass");

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(credentials);
        _mockSeidaClient.Setup(x => x.QueryStatusAsync(year, ticketNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.QueryStatusAsync(year, ticketNumber);

        // Assert
        result.Should().NotBeNull();
        result.StatusType.Should().Be(expectedResponse.StatusType);
    }

    [Fact]
    public async Task QueryStatusAsync_WithDifferentStatusTypes_ReturnsCorrectStatus()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        var responseWithStatus = TestDataFixtures.CreateValidQueryResponse(statusType: "RECHAZADO");
        var credentials = new SecurityCredentials("testuser", "testpass");

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(credentials);
        _mockSeidaClient.Setup(x => x.QueryStatusAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseWithStatus);

        // Act
        var result = await _service.QueryStatusAsync(request);

        // Assert
        result.StatusType.Should().Be("RECHAZADO");
    }
}
