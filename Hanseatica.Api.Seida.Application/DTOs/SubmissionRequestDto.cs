namespace Hanseatica.Api.Seida.Application.DTOs;

/// <summary>
/// DTO for submitting a file to SEIDA service
/// </summary>
public class SubmissionRequestDto
{
    public string TransactionNumber { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
}
