namespace Hanseatica.Api.Seida.Infrastructure.Services;

/// <summary>
/// Validator for SOL password (SolPassword) field with SUNAT requirements
/// </summary>
public static class SolPasswordValidator
{
    /// <summary>
    /// Validates SOL password is not empty
    /// </summary>
    /// <param name="solPassword">The password to validate</param>
    /// <exception cref="ArgumentException">If validation fails</exception>
    public static void Validate(string? solPassword)
    {
        if (string.IsNullOrWhiteSpace(solPassword))
            throw new ArgumentException("SolPassword cannot be empty", nameof(solPassword));

        if (solPassword.Length == 0)
            throw new ArgumentException("SolPassword must not be empty", nameof(solPassword));
    }

    /// <summary>
    /// Checks if SOL password is valid (not empty)
    /// </summary>
    public static bool IsValid(string? solPassword)
    {
        return !string.IsNullOrWhiteSpace(solPassword);
    }

    /// <summary>
    /// Gets validation error message for SOL password if invalid
    /// </summary>
    public static string? GetError(string? solPassword)
    {
        if (string.IsNullOrWhiteSpace(solPassword))
            return "SolPassword cannot be empty";

        return null;
    }
}
