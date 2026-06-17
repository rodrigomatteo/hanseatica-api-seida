namespace Hanseatica.Api.Seida.Application.Interfaces;

using DTOs;

/// <summary>
/// Service interface for submission operations
/// </summary>
public interface ISubmissionService
{
    Task<ReceiptDto> SubmitFileAsync(SubmissionRequestDto request, CancellationToken cancellationToken = default);
    Task<ReceiptDto> SubmitFileAsync(string transactionNumber, byte[] fileContent, CancellationToken cancellationToken = default);
}
