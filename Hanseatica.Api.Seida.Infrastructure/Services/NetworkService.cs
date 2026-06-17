namespace Hanseatica.Api.Seida.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;

/// <summary>
/// Service for network-related operations
/// </summary>
public class NetworkService
{
    private readonly ILogger<NetworkService> _logger;

    public NetworkService(ILogger<NetworkService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GetLocalIpAddress()
    {
        try
        {
            var ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var addr in ipAddresses)
            {
                if (addr.AddressFamily.ToString() == "InterNetwork")
                {
                    return addr.ToString();
                }
            }
            return "0.0.0.0";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving local IP address");
            return "0.0.0.0";
        }
    }

    public string GetMacAddress()
    {
        try
        {
            var ipAddress = GetLocalIpAddress();
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var adapter in networkInterfaces)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    var physicalAddress = adapter.GetPhysicalAddress();
                    var macAddress = string.Join(":", physicalAddress.GetAddressBytes().Select(x => x.ToString("X2")));

                    var properties = adapter.GetIPProperties();
                    foreach (var unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.ToString() == ipAddress)
                        {
                            _logger.LogInformation(
                                "Matched adapter: {AdapterName}, MAC: {MacAddress}, IP: {IpAddress}",
                                adapter.Name,
                                macAddress,
                                ipAddress);
                            return macAddress;
                        }
                    }
                }
            }

            return "00:00:00:00:00:00";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MAC address");
            return "00:00:00:00:00:00";
        }
    }
}
