namespace Hanseatica.Api.Seida.Application.Interfaces;

/// <summary>
/// Service interface for file operations
/// </summary>
public interface IFileService
{
    string GenerateUniqueFileName(string extension);
    string GenerateTransactionId();
    string GenerateAcuseFileName(string year, string ticket);
    string GenerateResponseFileName(string year, string ticket);
    Task SaveFileAsync(string filePath, byte[] content, CancellationToken cancellationToken = default);
    Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);
}
