namespace Hanseatica.Api.Seida.Application.DTOs;

/// <summary>
/// DTO for querying submission status
/// </summary>
public class QueryRequestDto
{
    public string Year { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
}
