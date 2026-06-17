namespace Hanseatica.Api.Seida.Domain.Entities;

/// <summary>
/// Represents a query to check submission status
/// </summary>
public class Query
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Year { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Pending";

    public Query() { }

    public Query(string year, string ticketNumber)
    {
        if (string.IsNullOrWhiteSpace(year))
            throw new ArgumentException("Year cannot be empty", nameof(year));
        if (string.IsNullOrWhiteSpace(ticketNumber))
            throw new ArgumentException("TicketNumber cannot be empty", nameof(ticketNumber));

        Year = year;
        TicketNumber = ticketNumber;
    }
}
