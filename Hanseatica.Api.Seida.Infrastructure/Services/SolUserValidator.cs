namespace Hanseatica.Api.Seida.Infrastructure.Services;

using System.Text.RegularExpressions;

/// <summary>
/// Validator for SOL user (UsuarioSOL) field with SUNAT requirements
/// </summary>
public static class SolUserValidator
{
    private const int MaxLength = 15;
    private static readonly Regex SolUserPattern = new(@"^[A-Z0-9]+$", RegexOptions.Compiled);

    /// <summary>
    /// Validates and normalizes SOL user to uppercase
    /// </summary>
    /// <param name="usuarioSOL">The SOL user to validate and normalize</param>
    /// <returns>Normalized SOL user in uppercase</returns>
    /// <exception cref="ArgumentException">If validation fails</exception>
    public static string ValidateAndNormalize(string? usuarioSOL)
    {
        if (string.IsNullOrWhiteSpace(usuarioSOL))
            throw new ArgumentException("UsuarioSOL cannot be empty", nameof(usuarioSOL));

        if (usuarioSOL.Length > MaxLength)
            throw new ArgumentException(
                $"UsuarioSOL exceeds maximum length of {MaxLength} characters, got: {usuarioSOL.Length}",
                nameof(usuarioSOL));

        var normalized = usuarioSOL.Trim().ToUpperInvariant();

        if (!SolUserPattern.IsMatch(normalized))
            throw new ArgumentException(
                $"UsuarioSOL must contain only uppercase letters and digits, got: {usuarioSOL}",
                nameof(usuarioSOL));

        return normalized;
    }

    /// <summary>
    /// Validates SOL user without normalization
    /// </summary>
    public static bool IsValid(string? usuarioSOL)
    {
        if (string.IsNullOrWhiteSpace(usuarioSOL))
            return false;

        if (usuarioSOL.Length > MaxLength)
            return false;

        var uppercase = usuarioSOL.ToUpperInvariant();
        return SolUserPattern.IsMatch(uppercase);
    }

    /// <summary>
    /// Gets validation error messages for SOL user
    /// </summary>
    public static IEnumerable<string> GetErrors(string? usuarioSOL)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(usuarioSOL))
        {
            errors.Add("UsuarioSOL cannot be empty");
            return errors;
        }

        if (usuarioSOL.Length > MaxLength)
            errors.Add($"UsuarioSOL exceeds maximum length of {MaxLength} characters");

        var uppercase = usuarioSOL.ToUpperInvariant();
        if (!SolUserPattern.IsMatch(uppercase))
            errors.Add("UsuarioSOL must contain only uppercase letters and digits");

        return errors;
    }
}
