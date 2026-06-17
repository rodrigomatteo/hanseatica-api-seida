namespace Hanseatica.Api.Seida.Domain.ValueObjects;

/// <summary>
/// Value object representing security credentials for SEIDA authentication
/// </summary>
public class SecurityCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public SecurityCredentials() { }

    public SecurityCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        Username = username;
        Password = password;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
