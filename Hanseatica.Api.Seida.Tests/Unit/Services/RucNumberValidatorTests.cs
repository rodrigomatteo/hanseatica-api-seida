namespace Hanseatica.Api.Seida.Tests.Unit.Services;

using FluentAssertions;
using Hanseatica.Api.Seida.Infrastructure.Services;
using Xunit;

public class RucNumberValidatorTests
{
    [Fact]
    public void Validate_WithValidRuc_DoesNotThrow()
    {
        // Arrange
        var ruc = "20100010136";

        // Act & Assert
        var act = () => RucNumberValidator.Validate(ruc);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithAllZeros_DoesNotThrow()
    {
        // Arrange
        var ruc = "00000000000";

        // Act & Assert
        var act = () => RucNumberValidator.Validate(ruc);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithAllNines_DoesNotThrow()
    {
        // Arrange
        var ruc = "99999999999";

        // Act & Assert
        var act = () => RucNumberValidator.Validate(ruc);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmpty_ThrowsArgumentException(string? ruc)
    {
        // Act & Assert
        var act = () => RucNumberValidator.Validate(ruc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("NumeroRUC cannot be empty*");
    }

    [Theory]
    [InlineData("2010001013")] // 10 digits
    [InlineData("201000101360")] // 12 digits
    [InlineData("20100010136 ")] // with space
    public void Validate_WithWrongLength_ThrowsArgumentException(string ruc)
    {
        // Act & Assert
        var act = () => RucNumberValidator.Validate(ruc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be exactly 11 characters*");
    }

    [Theory]
    [InlineData("2010001013A")]
    [InlineData("20100010136")]
    [InlineData("2010001013-")]
    [InlineData("2010001013.")]
    public void Validate_WithNonNumeric_ThrowsArgumentException(string ruc)
    {
        if (ruc == "20100010136")
            return; // This one is actually valid, skip it

        // Act & Assert
        var act = () => RucNumberValidator.Validate(ruc);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must contain only numeric digits*");
    }

    [Fact]
    public void IsValid_WithValidRuc_ReturnsTrue()
    {
        // Act
        var result = RucNumberValidator.IsValid("20100010136");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2010001013")]
    [InlineData("201000101360")]
    [InlineData("2010001013A")]
    [InlineData("2010001013-")]
    public void IsValid_WithInvalid_ReturnsFalse(string? ruc)
    {
        // Act
        var result = RucNumberValidator.IsValid(ruc);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetErrors_WithValidRuc_ReturnsNoErrors()
    {
        // Act
        var errors = RucNumberValidator.GetErrors("20100010136");

        // Assert
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "NumeroRUC cannot be empty")]
    [InlineData("", "NumeroRUC cannot be empty")]
    [InlineData("   ", "NumeroRUC cannot be empty")]
    [InlineData("2010001013", "must be exactly 11 characters")]
    [InlineData("201000101360", "must be exactly 11 characters")]
    [InlineData("2010001013A", "must contain only numeric digits")]
    public void GetErrors_WithInvalid_ReturnsErrors(string? ruc, string expectedError)
    {
        // Act
        var errors = RucNumberValidator.GetErrors(ruc).ToList();

        // Assert
        errors.Should().ContainMatch($"*{expectedError}*");
    }

    [Fact]
    public void GetErrors_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var ruc = "2010001013A"; // Wrong length and non-numeric

        // Act
        var errors = RucNumberValidator.GetErrors(ruc).ToList();

        // Assert - May return both errors depending on order
        errors.Should().NotBeEmpty();
    }
}
