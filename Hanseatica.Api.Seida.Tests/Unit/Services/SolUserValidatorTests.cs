namespace Hanseatica.Api.Seida.Tests.Unit.Services;

using FluentAssertions;
using Hanseatica.Api.Seida.Infrastructure.Services;
using Xunit;

public class SolUserValidatorTests
{
    [Fact]
    public void ValidateAndNormalize_WithValidUser_ReturnsUppercase()
    {
        // Arrange
        var solUser = "pilotons";

        // Act
        var result = SolUserValidator.ValidateAndNormalize(solUser);

        // Assert
        result.Should().Be("PILOTONS");
    }

    [Fact]
    public void ValidateAndNormalize_WithMixedCase_ReturnsUppercase()
    {
        // Arrange
        var solUser = "PiLoToNs";

        // Act
        var result = SolUserValidator.ValidateAndNormalize(solUser);

        // Assert
        result.Should().Be("PILOTONS");
    }

    [Fact]
    public void ValidateAndNormalize_WithMaxLength_Returns()
    {
        // Arrange
        var solUser = "SOLUSER1234"; // 11 chars (within 15 max)

        // Act
        var result = SolUserValidator.ValidateAndNormalize(solUser);

        // Assert
        result.Should().Be("SOLUSER1234");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndNormalize_WithEmpty_ThrowsArgumentException(string? solUser)
    {
        // Act & Assert
        var act = () => SolUserValidator.ValidateAndNormalize(solUser);
        act.Should().Throw<ArgumentException>()
            .WithMessage("UsuarioSOL cannot be empty*");
    }

    [Fact]
    public void ValidateAndNormalize_ExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var solUser = "SOLUSER1234567890"; // 16 chars - exceeds max of 15

        // Act & Assert
        var act = () => SolUserValidator.ValidateAndNormalize(solUser);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum length of 15 characters*");
    }

    [Theory]
    [InlineData("SOLUSER-123")]
    [InlineData("SOLUSER 123")]
    [InlineData("SOLUSER.123")]
    [InlineData("SOLUSER@123")]
    public void ValidateAndNormalize_WithInvalidCharacters_ThrowsArgumentException(string solUser)
    {
        // Act & Assert
        var act = () => SolUserValidator.ValidateAndNormalize(solUser);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must contain only uppercase letters and digits*");
    }

    [Fact]
    public void IsValid_WithValidUser_ReturnsTrue()
    {
        // Act
        var result = SolUserValidator.IsValid("PILOTONS");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithLowercaseUser_ReturnsTrue()
    {
        // Act
        var result = SolUserValidator.IsValid("pilotons");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithMaxLength_ReturnsTrue()
    {
        // Act
        var result = SolUserValidator.IsValid("SOLUSER12345");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SOLUSER123456789X")] // 16 chars - exceeds max
    [InlineData("SOLUSER-123")]
    public void IsValid_WithInvalid_ReturnsFalse(string? solUser)
    {
        // Act
        var result = SolUserValidator.IsValid(solUser);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetErrors_WithValidUser_ReturnsNoErrors()
    {
        // Act
        var errors = SolUserValidator.GetErrors("PILOTONS");

        // Assert
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "UsuarioSOL cannot be empty")]
    [InlineData("", "UsuarioSOL cannot be empty")]
    [InlineData("   ", "UsuarioSOL cannot be empty")]
    [InlineData("SOLUSER1234567890", "exceeds maximum length")]
    [InlineData("SOLUSER-123", "must contain only uppercase")]
    public void GetErrors_WithInvalid_ReturnsErrors(string? solUser, string expectedError)
    {
        // Act
        var errors = SolUserValidator.GetErrors(solUser).ToList();

        // Assert
        errors.Should().ContainMatch($"*{expectedError}*");
    }

    [Fact]
    public void GetErrors_WithMultipleErrors_ReturnsAllErrors()
    {
        // Act
        var errors = SolUserValidator.GetErrors("SOLUSER123456-TEST").ToList();

        // Assert
        errors.Should().HaveCountGreaterThan(1);
    }
}
