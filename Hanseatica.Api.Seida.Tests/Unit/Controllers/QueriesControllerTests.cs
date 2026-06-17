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

public class QueriesControllerTests
{
    private readonly Mock<IQueryService> _mockQueryService;
    private readonly Mock<ILogger<QueriesController>> _mockLogger;
    private readonly QueriesController _controller;

    public QueriesControllerTests()
    {
        _mockQueryService = new Mock<IQueryService>();
        _mockLogger = new Mock<ILogger<QueriesController>>();

        _controller = new QueriesController(
            _mockQueryService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task QueryStatus_WithValidRequest_ReturnsOkWithResponse()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        var expectedResponse = TestDataFixtures.CreateValidQueryResponse();

        _mockQueryService.Setup(x => x.QueryStatusAsync(
            It.IsAny<QueryRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.QueryStatus(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<QueryResponseDto>().Subject;
        returnedResponse.StatusType.Should().Be(expectedResponse.StatusType);
    }

    [Fact]
    public async Task QueryStatus_WithNullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.QueryStatus(null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task QueryStatus_WithMissingYear_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        request.Year = "";

        // Act
        var result = await _controller.QueryStatus(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task QueryStatus_WithMissingTicketNumber_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();
        request.TicketNumber = "";

        // Act
        var result = await _controller.QueryStatus(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task QueryStatus_WhenServiceThrows_ReturnsStatusCode500()
    {
        // Arrange
        var request = TestDataFixtures.CreateValidQueryRequest();

        _mockQueryService.Setup(x => x.QueryStatusAsync(
            It.IsAny<QueryRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.QueryStatus(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task QueryStatusByParams_WithValidParams_ReturnsOkWithResponse()
    {
        // Arrange
        var year = "2024";
        var ticketNumber = "0000000123";
        var expectedResponse = TestDataFixtures.CreateValidQueryResponse();

        _mockQueryService.Setup(x => x.QueryStatusAsync(
            It.IsAny<QueryRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.QueryStatusByParams(year, ticketNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<QueryResponseDto>().Subject;
        returnedResponse.StatusType.Should().Be(expectedResponse.StatusType);
    }

    [Fact]
    public async Task QueryStatusByParams_WithMissingYear_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.QueryStatusByParams("", "0000000123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task QueryStatusByParams_WithMissingTicketNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.QueryStatusByParams("2024", "", CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task QueryStatusByParams_WhenServiceThrows_ReturnsStatusCode500()
    {
        // Arrange
        _mockQueryService.Setup(x => x.QueryStatusAsync(
            It.IsAny<QueryRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.QueryStatusByParams("2024", "0000000123", CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task QueryStatusByParams_WithDifferentStatusTypes_ReturnsCorrectStatus()
    {
        // Arrange
        var year = "2024";
        var ticketNumber = "0000000123";
        var expectedResponse = TestDataFixtures.CreateValidQueryResponse(statusType: "RECHAZADO");

        _mockQueryService.Setup(x => x.QueryStatusAsync(
            It.IsAny<QueryRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.QueryStatusByParams(year, ticketNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<QueryResponseDto>().Subject;
        returnedResponse.StatusType.Should().Be("RECHAZADO");
    }
}
