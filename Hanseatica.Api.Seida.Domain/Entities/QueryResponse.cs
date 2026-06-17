namespace Hanseatica.Api.Seida.Domain.Entities;

using Enums;

/// <summary>
/// Represents a response from a SEIDA query operation
/// </summary>
public class QueryResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string QueryId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public QueryStatusType StatusType { get; set; } = QueryStatusType.Unknown;
    public byte[]? ResponseData { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public QueryResponse() { }

    public QueryResponse(string queryId, string description, QueryStatusType statusType)
    {
        if (string.IsNullOrWhiteSpace(queryId))
            throw new ArgumentException("QueryId cannot be empty", nameof(queryId));

        QueryId = queryId;
        Description = description;
        StatusType = statusType;
    }
}
