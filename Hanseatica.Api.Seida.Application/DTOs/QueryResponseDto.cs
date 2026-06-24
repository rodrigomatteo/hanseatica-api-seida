namespace Hanseatica.Api.Seida.Application.DTOs;

/// <summary>
/// DTO representing a query response from SEIDA
/// </summary>
public class QueryResponseDto
{
    public string Description { get; set; } = string.Empty;
    public string StatusType { get; set; } = string.Empty;
    public byte[]? ResponseData { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
