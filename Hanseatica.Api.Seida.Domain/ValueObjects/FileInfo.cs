namespace Hanseatica.Api.Seida.Domain.ValueObjects;

/// <summary>
/// Value object representing file information
/// </summary>
public class FileInfo
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/zip";

    public FileInfo() { }

    public FileInfo(string fileName, byte[] content, string contentType = "application/zip")
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be empty", nameof(fileName));
        if (content == null || content.Length == 0)
            throw new ArgumentException("Content cannot be empty", nameof(content));

        FileName = fileName;
        Content = content;
        ContentType = contentType;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(FileName) && Content.Length > 0;
}
