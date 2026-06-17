namespace Hanseatica.Api.Seida.Application.Interfaces;

using DTOs;

/// <summary>
/// Interface for SEIDA client operations
/// </summary>
public interface ISeidaClient
{
    Task<ReceiptDto> SubmitFileAsync(string transactionNumber, byte[] fileContent, CancellationToken cancellationToken = default);
    Task<QueryResponseDto> QueryStatusAsync(string year, string ticketNumber, CancellationToken cancellationToken = default);
}
