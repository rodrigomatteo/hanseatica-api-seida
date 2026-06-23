namespace Hanseatica.Api.Seida.Tests.Unit.Services;

using FluentAssertions;
using Hanseatica.Api.Seida.Infrastructure.Services;
using Xunit;

public class SolPasswordValidatorTests
{
    [Fact]
    public void Validate_WithValidPassword_DoesNotThrow()
    {
        // Arrange
        var password = "TESTPAS1";

        // Act & Assert
        var act = () => SolPasswordValidator.Validate(password);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithComplexPassword_DoesNotThrow()
    {
        // Arrange
        var password = "MySecure@Password123";

        // Act & Assert
        var act = () => SolPasswordValidator.Validate(password);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmpty_ThrowsArgumentException(string? password)
    {
        // Act & Assert
        var act = () => SolPasswordValidator.Validate(password);
        act.Should().Throw<ArgumentException>()
            .WithMessage("SolPassword cannot be empty*");
    }

    [Fact]
    public void IsValid_WithValidPassword_ReturnsTrue()
    {
        // Act
        var result = SolPasswordValidator.IsValid("TESTPAS1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithComplexPassword_ReturnsTrue()
    {
        // Act
        var result = SolPasswordValidator.IsValid("MySecure@Password123!#$");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithSingleCharacter_ReturnsTrue()
    {
        // Act
        var result = SolPasswordValidator.IsValid("x");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_WithEmpty_ReturnsFalse(string? password)
    {
        // Act
        var result = SolPasswordValidator.IsValid(password);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetError_WithValidPassword_ReturnsNull()
    {
        // Act
        var error = SolPasswordValidator.GetError("TESTPAS1");

        // Assert
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetError_WithEmpty_ReturnsErrorMessage(string? password)
    {
        // Act
        var error = SolPasswordValidator.GetError(password);

        // Assert
        error.Should().Be("SolPassword cannot be empty");
    }
}
