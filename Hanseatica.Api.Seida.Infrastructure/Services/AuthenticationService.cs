namespace Hanseatica.Api.Seida.Infrastructure.Services;

using Application.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Implementation of authentication service
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private const string AesKeyDefault = "3F34F69EB6EA530";

    public AuthenticationService(IConfiguration configuration, ILogger<AuthenticationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public SecurityCredentials GetCredentials()
    {
        var username = _configuration["Seida:Username"] ?? _configuration["Authentication:Username"];
        var password = _configuration["Seida:Password"] ?? _configuration["Authentication:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Authentication credentials not configured properly");
            return new SecurityCredentials();
        }

        return new SecurityCredentials(username, password);
    }

    public string EncryptCredentials(string username, string password)
    {
        try
        {
            var credentialString = $"{username}:{password}";
            var clearBytes = Encoding.Unicode.GetBytes(credentialString);

            using (var aes = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(
                    AesKeyDefault,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

                aes.Key = pdb.GetBytes(32);
                aes.IV = pdb.GetBytes(16);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting credentials");
            throw;
        }
    }

    public string DecryptCredentials(string encryptedData)
    {
        try
        {
            var cipherBytes = Convert.FromBase64String(encryptedData);

            using (var aes = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(
                    AesKeyDefault,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

                aes.Key = pdb.GetBytes(32);
                aes.IV = pdb.GetBytes(16);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    return Encoding.Unicode.GetString(ms.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting credentials");
            throw;
        }
    }
}
