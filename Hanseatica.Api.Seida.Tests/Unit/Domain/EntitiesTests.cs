namespace Hanseatica.Api.Seida.Tests.Unit.Domain;

using Hanseatica.Api.Seida.Domain.Entities;
using Hanseatica.Api.Seida.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class SubmissionEntityTests
{
    [Fact]
    public void CreateSubmission_WithValidData_SucceedsAndHasDefaults()
    {
        // Arrange & Act
        var submission = new Submission("TXN123", "file.zip", 1024);

        // Assert
        submission.TransactionNumber.Should().Be("TXN123");
        submission.FileName.Should().Be("file.zip");
        submission.FileSize.Should().Be(1024);
        submission.Status.Should().Be("Pending");
        submission.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        submission.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateSubmission_WithEmptyTransactionNumber_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Submission("", "file.zip", 1024));
    }

    [Fact]
    public void CreateSubmission_WithEmptyFileName_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Submission("TXN123", "", 1024));
    }

    [Fact]
    public void CreateSubmission_WithZeroFileSize_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Submission("TXN123", "file.zip", 0));
    }

    [Fact]
    public void CreateSubmission_WithNegativeFileSize_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Submission("TXN123", "file.zip", -100));
    }
}

public class ReceiptEntityTests
{
    [Fact]
    public void CreateReceipt_WithErrors_HasErrorsIsTrue()
    {
        // Arrange
        var receipt = new Receipt();
        receipt.Errors.Add(new ErrorDetail { Code = "E001", Description = "Error", Type = "Error" });

        // Act & Assert
        receipt.HasErrors.Should().BeTrue();
        receipt.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void CreateReceipt_WithWarnings_HasWarningsIsTrue()
    {
        // Arrange
        var receipt = new Receipt();
        receipt.Warnings.Add(new ErrorDetail { Code = "W001", Description = "Warning", Type = "Warning" });

        // Act & Assert
        receipt.HasWarnings.Should().BeTrue();
        receipt.Warnings.Should().HaveCount(1);
    }

    [Fact]
    public void CreateReceipt_WithNoErrors_HasErrorsIsFalse()
    {
        // Arrange & Act
        var receipt = new Receipt();

        // Assert
        receipt.HasErrors.Should().BeFalse();
        receipt.Errors.Should().BeEmpty();
    }
}

public class QueryEntityTests
{
    [Fact]
    public void CreateQuery_WithValidData_Succeeds()
    {
        // Arrange & Act
        var query = new Query("2024", "0000000123");

        // Assert
        query.Year.Should().Be("2024");
        query.TicketNumber.Should().Be("0000000123");
        query.Status.Should().Be("Pending");
        query.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateQuery_WithEmptyYear_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Query("", "0000000123"));
    }

    [Fact]
    public void CreateQuery_WithEmptyTicketNumber_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Query("2024", ""));
    }
}

public class SecurityCredentialsValueObjectTests
{
    [Fact]
    public void CreateCredentials_WithValidData_Succeeds()
    {
        // Arrange & Act
        var credentials = new SecurityCredentials("user", "pass");

        // Assert
        credentials.Username.Should().Be("user");
        credentials.Password.Should().Be("pass");
        credentials.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCredentials_WithEmptyUsername_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SecurityCredentials("", "pass"));
    }

    [Fact]
    public void CreateCredentials_WithEmptyPassword_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SecurityCredentials("user", ""));
    }

    [Fact]
    public void EmptyCredentials_IsInvalid()
    {
        // Arrange & Act
        var credentials = new SecurityCredentials();

        // Assert
        credentials.IsValid.Should().BeFalse();
    }
}

public class FileInfoValueObjectTests
{
    [Fact]
    public void CreateFileInfo_WithValidData_Succeeds()
    {
        // Arrange & Act
        var fileInfo = new FileInfo("test.zip", new byte[100], "application/zip");

        // Assert
        fileInfo.FileName.Should().Be("test.zip");
        fileInfo.Content.Should().HaveCount(100);
        fileInfo.ContentType.Should().Be("application/zip");
        fileInfo.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateFileInfo_WithEmptyFileName_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FileInfo("", new byte[100]));
    }

    [Fact]
    public void CreateFileInfo_WithEmptyContent_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FileInfo("test.zip", Array.Empty<byte>()));
    }

    [Fact]
    public void CreateFileInfo_WithDefaultContentType()
    {
        // Arrange & Act
        var fileInfo = new FileInfo("test.zip", new byte[100]);

        // Assert
        fileInfo.ContentType.Should().Be("application/zip");
    }
}
