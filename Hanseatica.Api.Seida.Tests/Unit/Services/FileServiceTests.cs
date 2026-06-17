namespace Hanseatica.Api.Seida.Tests.Unit.Services;

using Hanseatica.Api.Seida.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class FileServiceTests
{
    private readonly Mock<ILogger<FileService>> _mockLogger;
    private readonly FileService _service;

    public FileServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileService>>();
        _service = new FileService(_mockLogger.Object);
    }

    [Fact]
    public void GenerateUniqueFileName_ReturnsValidFileName()
    {
        // Act
        var fileName = _service.GenerateUniqueFileName("zip");

        // Assert
        fileName.Should().NotBeNullOrEmpty();
        fileName.Should().EndWith(".zip");
        fileName.Should().Match("*_*.zip");
    }

    [Fact]
    public void GenerateTransactionId_ReturnsValidId()
    {
        // Act
        var transactionId = _service.GenerateTransactionId();

        // Assert
        transactionId.Should().NotBeNullOrEmpty();
        transactionId.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void GenerateTransactionId_GeneratesUniqueIds()
    {
        // Act
        var id1 = _service.GenerateTransactionId();
        var id2 = _service.GenerateTransactionId();

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void GenerateAcuseFileName_ReturnsCorrectFormat()
    {
        // Arrange
        var year = "2024";
        var ticket = "0000000123";

        // Act
        var fileName = _service.GenerateAcuseFileName(year, ticket);

        // Assert
        fileName.Should().Be("seida_acuse_2024_0000000123.xml");
    }

    [Fact]
    public void GenerateAcuseFileName_TrimTicketNumber()
    {
        // Arrange
        var year = "2024";
        var ticket = "  0000000123  ";

        // Act
        var fileName = _service.GenerateAcuseFileName(year, ticket);

        // Assert
        fileName.Should().Be("seida_acuse_2024_0000000123.xml");
    }

    [Fact]
    public void GenerateResponseFileName_ReturnsValidFormat()
    {
        // Arrange
        var year = "2024";
        var ticket = "0000000123";

        // Act
        var fileName = _service.GenerateResponseFileName(year, ticket);

        // Assert
        fileName.Should().StartWith("seida_respuesta_2024_");
        fileName.Should().EndWith("_0000000123.xml");
        fileName.Should().Contain("_");
    }

    [Fact]
    public async Task SaveFileAsync_SavesFileSuccessfully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var filePath = Path.Combine(tempDir, "test_file.bin");
        var content = new byte[] { 1, 2, 3, 4, 5 };

        try
        {
            // Act
            await _service.SaveFileAsync(filePath, content);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            var savedContent = await File.ReadAllBytesAsync(filePath);
            savedContent.Should().Equal(content);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "subdir");
        var filePath = Path.Combine(tempDir, "test_file.bin");
        var content = new byte[] { 1, 2, 3 };

        try
        {
            // Act
            await _service.SaveFileAsync(filePath, content);

            // Assert
            Directory.Exists(tempDir).Should().BeTrue();
            File.Exists(filePath).Should().BeTrue();
        }
        finally
        {
            var parentDir = Path.GetDirectoryName(tempDir);
            if (Directory.Exists(parentDir))
                Directory.Delete(parentDir, true);
        }
    }

    [Fact]
    public async Task ReadFileAsync_ReadsFileSuccessfully()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var expectedContent = new byte[] { 10, 20, 30, 40, 50 };

        try
        {
            await File.WriteAllBytesAsync(tempFile, expectedContent);

            // Act
            var content = await _service.ReadFileAsync(tempFile);

            // Assert
            content.Should().Equal(expectedContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadFileAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid().ToString() + ".bin");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.ReadFileAsync(nonExistentFile));
    }

    [Fact]
    public async Task SaveFileAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".bin");
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var content = new byte[1024];

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _service.SaveFileAsync(tempFile, content, cts.Token));
    }
}
