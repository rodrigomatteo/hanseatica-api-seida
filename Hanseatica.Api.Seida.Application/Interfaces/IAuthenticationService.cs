namespace Hanseatica.Api.Seida.Application.Interfaces;

using Domain.ValueObjects;

/// <summary>
/// Service interface for authentication
/// </summary>
public interface IAuthenticationService
{
    SecurityCredentials GetCredentials();
    string EncryptCredentials(string username, string password);
    string DecryptCredentials(string encryptedData);
}
