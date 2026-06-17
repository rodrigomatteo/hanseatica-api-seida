namespace Hanseatica.Api.Seida.Domain.Entities;

/// <summary>
/// Represents a receipt/acknowledgment from SEIDA service
/// </summary>
public class Receipt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SubmissionYear { get; set; } = string.Empty;
    public string IssuerDocument { get; set; } = string.Empty;
    public DateTime ReceptionDate { get; set; }
    public string DocumentHash { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public List<ErrorDetail> Errors { get; set; } = new();
    public List<ErrorDetail> Warnings { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;
}

public class ErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
