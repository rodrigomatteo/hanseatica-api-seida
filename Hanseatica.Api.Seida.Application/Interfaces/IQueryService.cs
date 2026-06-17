namespace Hanseatica.Api.Seida.Application.Interfaces;

using DTOs;

/// <summary>
/// Service interface for query operations
/// </summary>
public interface IQueryService
{
    Task<QueryResponseDto> QueryStatusAsync(QueryRequestDto request, CancellationToken cancellationToken = default);
    Task<QueryResponseDto> QueryStatusAsync(string year, string ticketNumber, CancellationToken cancellationToken = default);
}
