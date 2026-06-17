namespace Hanseatica.Api.Seida.Infrastructure.Services;

using Application.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of file service
/// </summary>
public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GenerateUniqueFileName(string extension)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N");
        return $"{timestamp}_{guid}.{extension}";
    }

    public string GenerateTransactionId()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N").Substring(0, 12);
    }

    public string GenerateAcuseFileName(string year, string ticket)
    {
        return $"seida_acuse_{year}_{ticket.Trim()}.xml";
    }

    public string GenerateResponseFileName(string year, string ticket)
    {
        var unique = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"seida_respuesta_{year}_{unique}_{ticket.Trim()}.xml";
    }

    public async Task SaveFileAsync(string filePath, byte[] content, CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(filePath, content, cancellationToken);
            _logger.LogInformation("File saved successfully: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var content = await File.ReadAllBytesAsync(filePath, cancellationToken);
            _logger.LogInformation("File read successfully: {FilePath}", filePath);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            throw;
        }
    }
}
