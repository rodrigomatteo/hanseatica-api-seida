namespace Hanseatica.Api.Seida.Tests.Unit.Controllers;

using Hanseatica.Api.Seida.Api.Controllers;
using Hanseatica.Api.Seida.Application.DTOs;
using Hanseatica.Api.Seida.Application.Interfaces;
using Hanseatica.Api.Seida.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class SubmissionsControllerTests
{
    private readonly Mock<ISubmissionService> _mockSubmissionService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<ILogger<SubmissionsController>> _mockLogger;
    private readonly SubmissionsController _controller;

    public SubmissionsControllerTests()
    {
        _mockSubmissionService = new Mock<ISubmissionService>();
        _mockFileService = new Mock<IFileService>();
        _mockLogger = new Mock<ILogger<SubmissionsController>>();

        _controller = new SubmissionsController(
            _mockSubmissionService.Object,
            _mockFileService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SubmitFile_WithValidRequest_ReturnsOkWithReceipt()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();
        var expectedReceipt = TestDataFixtures.CreateValidReceipt();

        _mockSubmissionService.Setup(x => x.SubmitFileAsync(
            It.IsAny<SubmissionRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.SubmitFile(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReceipt = okResult.Value.Should().BeOfType<ReceiptDto>().Subject;
        returnedReceipt.TicketNumber.Should().Be(expectedReceipt.TicketNumber);
    }

    [Fact]
    public async Task SubmitFile_WithNullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SubmitFile(null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitFile_WithEmptyFileContent_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();
        request.FileContent = Array.Empty<byte>();

        // Act
        var result = await _controller.SubmitFile(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitFile_WhenServiceThrows_ReturnsStatusCode500()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidSubmissionRequest();

        _mockSubmissionService.Setup(x => x.SubmitFileAsync(
            It.IsAny<SubmissionRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.SubmitFile(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public void GenerateTransactionId_ReturnsOkWithId()
    {
        // Arrange
        var expectedId = "2024061412345678";
        _mockFileService.Setup(x => x.GenerateTransactionId())
            .Returns(expectedId);

        // Act
        var result = _controller.GenerateTransactionId();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        var returnValue = okResult.Value;
        var valueType = returnValue?.GetType();

        // Get all properties to debug
        var properties = valueType?.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        properties.Should().NotBeEmpty();

        // Try to find the transactionId property (case-sensitive)
        var property = properties?.FirstOrDefault(p => p.Name == "transactionId");
        property.Should().NotBeNull("transactionId property should exist on the returned object");

        var transactionId = property?.GetValue(returnValue) as string;
        transactionId.Should().Be(expectedId);
    }

    [Fact]
    public async Task SubmitFileWithTransaction_WithValidFormFile_ReturnsOkWithReceipt()
    {
        // Arrange
        var transactionNumber = "TXN123456";
        var fileContent = new byte[1024];
        var fileName = "test.zip";
        var expectedReceipt = TestDataFixtures.CreateValidReceipt();

        var mockFile = CreateMockFormFile(fileName, fileContent);

        _mockSubmissionService.Setup(x => x.SubmitFileAsync(
            It.IsAny<SubmissionRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.SubmitFileWithTransaction(transactionNumber, mockFile, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReceipt = okResult.Value.Should().BeOfType<ReceiptDto>().Subject;
        returnedReceipt.TicketNumber.Should().Be(expectedReceipt.TicketNumber);
    }

    [Fact]
    public async Task SubmitFileWithTransaction_WithMissingTransactionNumber_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.zip", new byte[1024]);

        // Act
        var result = await _controller.SubmitFileWithTransaction("", mockFile, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitFileWithTransaction_WithNullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SubmitFileWithTransaction("TXN123456", null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitFileWithTransaction_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.zip", Array.Empty<byte>());

        // Act
        var result = await _controller.SubmitFileWithTransaction("TXN123456", mockFile, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static Microsoft.AspNetCore.Http.IFormFile CreateMockFormFile(string fileName, byte[] content)
    {
        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        mockFile.Setup(x => x.FileName).Returns(fileName);
        mockFile.Setup(x => x.Length).Returns(content.Length);
        mockFile.Setup(x => x.OpenReadStream()).Returns(new MemoryStream(content));
        mockFile.Setup(x => x.CopyToAsync(
            It.IsAny<System.IO.Stream>(),
            It.IsAny<CancellationToken>()))
            .Callback<System.IO.Stream, CancellationToken>((stream, _) =>
            {
                stream.Write(content, 0, content.Length);
            })
            .Returns(Task.CompletedTask);

        return mockFile.Object;
    }
}
