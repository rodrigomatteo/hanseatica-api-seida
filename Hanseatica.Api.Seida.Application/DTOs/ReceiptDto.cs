namespace Hanseatica.Api.Seida.Application.DTOs;

/// <summary>
/// DTO representing a receipt/acknowledgment from SEIDA
/// </summary>
public class ReceiptDto
{
    public string SubmissionYear { get; set; } = string.Empty;
    public string IssuerDocument { get; set; } = string.Empty;
    public DateTime ReceptionDate { get; set; }
    public string DocumentHash { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public List<ErrorDetailDto> Errors { get; set; } = new();
    public List<ErrorDetailDto> Warnings { get; set; } = new();
    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;
}

public class ErrorDetailDto
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
