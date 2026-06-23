namespace Hanseatica.Api.Seida.Infrastructure.Services;

using System.Text.RegularExpressions;

/// <summary>
/// Validator for RUC number (NumeroRUC) field with SUNAT requirements
/// </summary>
public static class RucNumberValidator
{
    private const int ExactLength = 11;
    private static readonly Regex RucPattern = new(@"^\d{11}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates RUC number format and content
    /// </summary>
    /// <param name="numeroRuc">The RUC number to validate</param>
    /// <exception cref="ArgumentException">If validation fails</exception>
    public static void Validate(string? numeroRuc)
    {
        if (string.IsNullOrWhiteSpace(numeroRuc))
            throw new ArgumentException("NumeroRUC cannot be empty", nameof(numeroRuc));

        if (numeroRuc.Length != ExactLength)
            throw new ArgumentException(
                $"NumeroRUC must be exactly {ExactLength} characters, got: {numeroRuc.Length}",
                nameof(numeroRuc));

        if (!RucPattern.IsMatch(numeroRuc))
            throw new ArgumentException(
                $"NumeroRUC must contain only numeric digits, got: {numeroRuc}",
                nameof(numeroRuc));
    }

    /// <summary>
    /// Checks if RUC number is valid
    /// </summary>
    public static bool IsValid(string? numeroRuc)
    {
        return !string.IsNullOrWhiteSpace(numeroRuc) && RucPattern.IsMatch(numeroRuc);
    }

    /// <summary>
    /// Gets validation error messages for RUC number
    /// </summary>
    public static IEnumerable<string> GetErrors(string? numeroRuc)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(numeroRuc))
        {
            errors.Add("NumeroRUC cannot be empty");
            return errors;
        }

        if (numeroRuc.Length != ExactLength)
            errors.Add($"NumeroRUC must be exactly {ExactLength} characters");

        if (!RucPattern.IsMatch(numeroRuc))
            errors.Add("NumeroRUC must contain only numeric digits");

        return errors;
    }
}
