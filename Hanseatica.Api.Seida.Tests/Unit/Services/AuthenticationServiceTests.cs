namespace Hanseatica.Api.Seida.Tests.Unit.Services;

using Hanseatica.Api.Seida.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class AuthenticationServiceTests
{
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly AuthenticationService _service;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public AuthenticationServiceTests()
    {
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _service = new AuthenticationService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public void GetCredentials_WithValidConfig_ReturnsCredentials()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";

        _mockConfiguration.Setup(x => x["Seida:Username"]).Returns(username);
        _mockConfiguration.Setup(x => x["Seida:Password"]).Returns(password);

        // Act
        var credentials = _service.GetCredentials();

        // Assert
        credentials.Should().NotBeNull();
        credentials.Username.Should().Be(username);
        credentials.Password.Should().Be(password);
        credentials.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetCredentials_WithMissingUsername_ReturnsInvalidCredentials()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Seida:Username"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["Seida:Password"]).Returns("testpass");

        // Act
        var credentials = _service.GetCredentials();

        // Assert
        credentials.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetCredentials_WithEmptyPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Seida:Username"]).Returns("testuser");
        _mockConfiguration.Setup(x => x["Seida:Password"]).Returns("");

        // Act
        var credentials = _service.GetCredentials();

        // Assert
        credentials.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetCredentials_FallsBackToAuthenticationSection()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Seida:Username"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["Seida:Password"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["Authentication:Username"]).Returns("altuser");
        _mockConfiguration.Setup(x => x["Authentication:Password"]).Returns("altpass");

        // Act
        var credentials = _service.GetCredentials();

        // Assert
        credentials.Username.Should().Be("altuser");
        credentials.Password.Should().Be("altpass");
    }

    [Fact]
    public void EncryptCredentials_ProducesValidBase64Output()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";

        // Act
        var encrypted = _service.EncryptCredentials(username, password);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        // Should be valid base64
        var decodedBytes = Convert.FromBase64String(encrypted);
        decodedBytes.Should().NotBeEmpty();
    }

    [Fact]
    public void EncryptCredentials_SameInputProducesSameOutput()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";

        // Act
        var encrypted1 = _service.EncryptCredentials(username, password);
        var encrypted2 = _service.EncryptCredentials(username, password);

        // Assert
        encrypted1.Should().Be(encrypted2);
    }

    [Fact]
    public void DecryptCredentials_CanDecryptEncryptedData()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";
        var encrypted = _service.EncryptCredentials(username, password);

        // Act
        var decrypted = _service.DecryptCredentials(encrypted);

        // Assert
        decrypted.Should().Be($"{username}:{password}");
    }

    [Fact]
    public void DecryptCredentials_WithInvalidBase64_ThrowsException()
    {
        // Arrange
        var invalidBase64 = "not_valid_base64!!!";

        // Act & Assert
        Assert.Throws<FormatException>(() => _service.DecryptCredentials(invalidBase64));
    }

    [Fact]
    public void EncryptAndDecrypt_Roundtrip_PreservesOriginalData()
    {
        // Arrange
        var testCases = new[]
        {
            ("user1", "pass1"),
            ("SUNAT_USER", "COMPLEX_P@SSW0RD"),
            ("user.name@domain", "password!@#$%")
        };

        // Act & Assert
        foreach (var (username, password) in testCases)
        {
            var encrypted = _service.EncryptCredentials(username, password);
            var decrypted = _service.DecryptCredentials(encrypted);
            decrypted.Should().Be($"{username}:{password}");
        }
    }
}
