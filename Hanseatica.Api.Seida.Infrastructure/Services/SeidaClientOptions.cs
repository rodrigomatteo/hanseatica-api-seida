namespace Hanseatica.Api.Seida.Infrastructure.Services;

/// <summary>
/// Configuration options for SEIDA client
/// </summary>
public class SeidaClientOptions
{
    /// <summary>
    /// MAC address to include in composite username (format: XX:XX:XX:XX:XX:XX)
    /// </summary>
    public string MacAddress { get; set; } = "88:51:FB:4A:50:26";

    /// <summary>
    /// IP address to include in composite username (format: XXX.XXX.XXX.XXX)
    /// </summary>
    public string IpAddress { get; set; } = "192.168.1.45";

    /// <summary>
    /// Validates the options
    /// </summary>
    /// <returns>Collection of validation error messages, empty if valid</returns>
    public IEnumerable<string> Validate()
    {
        var errors = CompositeUsernameValidator.ValidateComponents(
            "20100010136",  // Dummy RUC for component validation
            "TEST",         // Dummy SOL for component validation
            MacAddress,
            IpAddress);

        return errors.Where(e => e.Contains("MacAddress") || e.Contains("IpAddress"));
    }
}
