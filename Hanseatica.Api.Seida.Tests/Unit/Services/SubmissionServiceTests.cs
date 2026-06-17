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

public class SubmissionServiceTests
{
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ISeidaClient> _mockSeidaClient;
    private readonly Mock<ILogger<SubmissionService>> _mockLogger;
    private readonly SubmissionService _service;

    public SubmissionServiceTests()
    {
        _mockFileService = new Mock<IFileService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockSeidaClient = new Mock<ISeidaClient>();
        _mockLogger = new Mock<ILogger<SubmissionService>>();

        _service = new SubmissionService(
            _mockFileService.Object,
            _mockAuthService.Object,
            _mockSeidaClient.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SubmitFileAsync_WithValidRequest_ReturnsReceipt()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();
        var expectedReceipt = TestDataFixtures.CreateValidReceipt();
        var credentials = new SecurityCredentials("testuser", "testpass");

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(credentials);
        _mockSeidaClient.Setup(x => x.SubmitFileAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _service.SubmitFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TicketNumber.Should().Be(expectedReceipt.TicketNumber);
        result.SubmissionYear.Should().Be(expectedReceipt.SubmissionYear);
        _mockSeidaClient.Verify(
            x => x.SubmitFileAsync(request.TransactionNumber, request.FileContent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitFileAsync_WithNullRequest_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.SubmitFileAsync((SubmissionRequestDto)null!));
    }

    [Fact]
    public async Task SubmitFileAsync_WithEmptyTransactionNumber_ThrowsValidationException()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();
        request.TransactionNumber = "";

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.SubmitFileAsync(request));
    }

    [Fact]
    public async Task SubmitFileAsync_WithEmptyFileContent_ThrowsValidationException()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();
        request.FileContent = Array.Empty<byte>();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.SubmitFileAsync(request));
    }

    [Fact]
    public async Task SubmitFileAsync_WithInvalidCredentials_ThrowsSeidaServiceException()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();
        var invalidCredentials = new SecurityCredentials();

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(invalidCredentials);

        // Act & Assert
        await Assert.ThrowsAsync<SeidaServiceException>(
            () => _service.SubmitFileAsync(request));
    }

    [Fact]
    public async Task SubmitFileAsync_WhenSeidaClientThrows_ThrowsSeidaServiceException()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();
        var credentials = new SecurityCredentials("testuser", "testpass");

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(credentials);
        _mockSeidaClient.Setup(x => x.SubmitFileAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SeidaServiceException>(
            () => _service.SubmitFileAsync(request));
        ex.Message.Should().Contain("Failed to submit file");
    }

    [Fact]
    public async Task SubmitFileAsync_WithByteOverload_ReturnsReceipt()
    {
        // Arrange
        var transactionNumber = Guid.NewGuid().ToString("N").Substring(0, 12);
        var fileContent = new byte[1024];
        var expectedReceipt = TestDataFixtures.CreateValidReceipt();
        var credentials = new SecurityCredentials("testuser", "testpass");

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(credentials);
        _mockSeidaClient.Setup(x => x.SubmitFileAsync(
            transactionNumber,
            fileContent,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _service.SubmitFileAsync(transactionNumber, fileContent);

        // Assert
        result.Should().NotBeNull();
        result.TicketNumber.Should().Be(expectedReceipt.TicketNumber);
    }

    [Fact]
    public async Task SubmitFileAsync_ReceiptWithErrors_ContainsErrors()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();
        var receiptWithErrors = TestDataFixtures.CreateValidReceipt(errorCount: 2);
        var credentials = new SecurityCredentials("testuser", "testpass");

        _mockAuthService.Setup(x => x.GetCredentials()).Returns(credentials);
        _mockSeidaClient.Setup(x => x.SubmitFileAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiptWithErrors);

        // Act
        var result = await _service.SubmitFileAsync(request);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }
}
