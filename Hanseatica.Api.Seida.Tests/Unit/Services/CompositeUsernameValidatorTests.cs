namespace Hanseatica.Api.Seida.Tests.Unit.Services;

using FluentAssertions;
using Hanseatica.Api.Seida.Infrastructure.Services;
using Xunit;

public class CompositeUsernameValidatorTests
{
    [Fact]
    public void IsValid_WithValidCompositeUsername_ReturnsTrue()
    {
        // Arrange
        var validUsername = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2";

        // Act
        var result = CompositeUsernameValidator.IsValid(validUsername);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_WithNullOrEmptyUsername_ReturnsFalse(string? username)
    {
        // Act
        var result = CompositeUsernameValidator.IsValid(username);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("20100010136PILOTONS")]  // Missing pipes and components
    [InlineData("20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45")]  // Missing version
    [InlineData("20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|1")]  // Wrong version
    [InlineData("2010001013PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2")]  // RUC too short
    [InlineData("20100010136PILOTONS|88:51:FB:4A:50:2|192.168.1.45|2")]  // Invalid MAC (missing segment)
    [InlineData("20100010136PILOTONS|88:51:FB:4A:50:ZZ|192.168.1.45|2")]  // Invalid MAC (non-hex)
    [InlineData("20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1|2")]  // Invalid IP (incomplete)
    [InlineData("20100010136PILOTONS|88:51:FB:4A:50:26|256.168.1.45|2")]  // Invalid IP (octet out of range)
    public void IsValid_WithInvalidCompositeUsername_ReturnsFalse(string invalidUsername)
    {
        // Act
        var result = CompositeUsernameValidator.IsValid(invalidUsername);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithValidCompositeUsername_ParsesCorrectly()
    {
        // Arrange
        var compositeUsername = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2";

        // Act
        var result = CompositeUsernameValidator.Parse(compositeUsername);

        // Assert
        result.Should().NotBeNull();
        result?.RUC.Should().Be("20100010136");
        result?.SOL.Should().Be("PILOTONS");
        result?.MAC.Should().Be("88:51:FB:4A:50:26");
        result?.IP.Should().Be("192.168.1.45");
        result?.Version.Should().Be(2);
    }

    [Fact]
    public void Parse_WithInvalidCompositeUsername_ReturnsNull()
    {
        // Arrange
        var invalidUsername = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|1";

        // Act
        var result = CompositeUsernameValidator.Parse(invalidUsername);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateComponents_WithValidComponents_ReturnsNoErrors()
    {
        // Arrange
        var numeroRuc = "20100010136";
        var usuarioSOL = "PILOTONS";
        var macAddress = "88:51:FB:4A:50:26";
        var ipAddress = "192.168.1.45";

        // Act
        var errors = CompositeUsernameValidator.ValidateComponents(numeroRuc, usuarioSOL, macAddress, ipAddress);

        // Assert
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "PILOTONS", "88:51:FB:4A:50:26", "192.168.1.45", "NumeroRUC cannot be empty")]
    [InlineData("2010001013", "PILOTONS", "88:51:FB:4A:50:26", "192.168.1.45", "NumeroRUC must be 11 digits")]
    [InlineData("2010001013X", "PILOTONS", "88:51:FB:4A:50:26", "192.168.1.45", "NumeroRUC must be 11 digits")]
    [InlineData("20100010136", "", "88:51:FB:4A:50:26", "192.168.1.45", "UsuarioSOL cannot be empty")]
    [InlineData("20100010136", "pilotons", "88:51:FB:4A:50:26", "192.168.1.45", "UsuarioSOL must contain only uppercase")]
    [InlineData("20100010136", "PILO-TONS", "88:51:FB:4A:50:26", "192.168.1.45", "UsuarioSOL must contain only uppercase")]
    [InlineData("20100010136", "PILOTONS", "", "192.168.1.45", "MacAddress cannot be empty")]
    [InlineData("20100010136", "PILOTONS", "88:51:FB:4A:50", "192.168.1.45", "MacAddress must be in format")]
    [InlineData("20100010136", "PILOTONS", "88:51:FB:4A:50:ZZ", "192.168.1.45", "MacAddress must be in format")]
    [InlineData("20100010136", "PILOTONS", "88:51:FB:4A:50:26", "", "IpAddress cannot be empty")]
    [InlineData("20100010136", "PILOTONS", "88:51:FB:4A:50:26", "192.168.1", "IpAddress must be in format")]
    [InlineData("20100010136", "PILOTONS", "88:51:FB:4A:50:26", "256.168.1.45", "IpAddress octets must be 0-255")]
    public void ValidateComponents_WithInvalidComponents_ReturnsErrorMessages(
        string numeroRuc, string usuarioSOL, string macAddress, string ipAddress, string expectedError)
    {
        // Act
        var errors = CompositeUsernameValidator.ValidateComponents(numeroRuc, usuarioSOL, macAddress, ipAddress);

        // Assert
        errors.Should().Contain(e => e.Contains(expectedError));
    }

    [Fact]
    public void ValidateComponents_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var numeroRuc = "invalid";
        var usuarioSOL = "invalid";
        var macAddress = "invalid";
        var ipAddress = "invalid";

        // Act
        var errors = CompositeUsernameValidator.ValidateComponents(numeroRuc, usuarioSOL, macAddress, ipAddress).ToList();

        // Assert
        errors.Should().HaveCount(4);
        errors.Should().Contain(e => e.Contains("NumeroRUC"));
        errors.Should().Contain(e => e.Contains("UsuarioSOL"));
        errors.Should().Contain(e => e.Contains("MacAddress"));
        errors.Should().Contain(e => e.Contains("IpAddress"));
    }

    [Fact]
    public void Parse_WithLowercaseMAC_StillParses()
    {
        // Arrange
        var compositeUsername = "20100010136PILOTONS|88:51:fb:4a:50:26|192.168.1.45|2";

        // Act
        var result = CompositeUsernameValidator.Parse(compositeUsername);

        // Assert
        result.Should().NotBeNull();
        result?.MAC.Should().Be("88:51:fb:4a:50:26");
    }
}
