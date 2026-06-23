namespace Hanseatica.Api.Seida.Infrastructure.Services;

using System.Text.RegularExpressions;

/// <summary>
/// Validator for SEIDA composite username format: RUC|SOL|MAC|IP|2
/// </summary>
public static class CompositeUsernameValidator
{
    private static readonly Regex CompositeUsernamePattern = new(
        @"^\d{11}[A-Z0-9]+\|([0-9A-F]{2}:){5}[0-9A-F]{2}\|(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\|2$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates if a composite username has the correct format
    /// </summary>
    public static bool IsValid(string? compositeUsername)
    {
        return !string.IsNullOrWhiteSpace(compositeUsername) &&
               CompositeUsernamePattern.IsMatch(compositeUsername);
    }

    /// <summary>
    /// Parses a composite username into its components
    /// </summary>
    /// <returns>Tuple of (RUC, SOL, MAC, IP, Version) or null if invalid</returns>
    public static (string RUC, string SOL, string MAC, string IP, int Version)? Parse(string? compositeUsername)
    {
        if (!IsValid(compositeUsername))
            return null;

        var parts = compositeUsername!.Split('|');
        if (parts.Length != 4)
            return null;

        var rucSol = parts[0];
        if (rucSol.Length < 11)
            return null;

        var ruc = rucSol.Substring(0, 11);
        var sol = rucSol.Substring(11);

        return (ruc, sol, parts[1], parts[2], int.Parse(parts[3]));
    }

    /// <summary>
    /// Validates individual components before building composite username
    /// </summary>
    public static IEnumerable<string> ValidateComponents(string numeroRuc, string usuarioSOL, string macAddress, string ipAddress)
    {
        var errors = new List<string>();

        // Validate RUC number
        errors.AddRange(RucNumberValidator.GetErrors(numeroRuc));

        // Validate SOL user
        errors.AddRange(SolUserValidator.GetErrors(usuarioSOL));

        if (string.IsNullOrWhiteSpace(macAddress))
            errors.Add("MacAddress cannot be empty");
        else if (!Regex.IsMatch(macAddress, @"^([0-9A-F]{2}:){5}[0-9A-F]{2}$", RegexOptions.IgnoreCase))
            errors.Add($"MacAddress must be in format XX:XX:XX:XX:XX:XX, got: {macAddress}");

        if (string.IsNullOrWhiteSpace(ipAddress))
            errors.Add("IpAddress cannot be empty");
        else if (!Regex.IsMatch(ipAddress, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
            errors.Add($"IpAddress must be in format XXX.XXX.XXX.XXX, got: {ipAddress}");
        else if (!IsValidIpAddress(ipAddress))
            errors.Add($"IpAddress octets must be 0-255, got: {ipAddress}");

        return errors;
    }

    private static bool IsValidIpAddress(string ipAddress)
    {
        var parts = ipAddress.Split('.');
        return parts.Length == 4 && parts.All(p =>
            int.TryParse(p, out var num) && num >= 0 && num <= 255);
    }
}
