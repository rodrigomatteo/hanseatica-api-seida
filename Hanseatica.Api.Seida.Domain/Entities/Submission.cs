namespace Hanseatica.Api.Seida.Domain.Entities;

/// <summary>
/// Represents a file submission to SEIDA service
/// </summary>
public class Submission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TransactionNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public string? Ticket { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public Submission() { }

    public Submission(string transactionNumber, string fileName, int fileSize)
    {
        if (string.IsNullOrWhiteSpace(transactionNumber))
            throw new ArgumentException("TransactionNumber cannot be empty", nameof(transactionNumber));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be empty", nameof(fileName));
        if (fileSize <= 0)
            throw new ArgumentException("FileSize must be greater than 0", nameof(fileSize));

        TransactionNumber = transactionNumber;
        FileName = fileName;
        FileSize = fileSize;
    }
}
