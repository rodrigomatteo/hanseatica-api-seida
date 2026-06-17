namespace Hanseatica.Api.Seida.Tests.Fixtures;

using Hanseatica.Api.Seida.Application.DTOs;

/// <summary>
/// Fixtures for test data
/// </summary>
public static class TestDataFixtures
{
    public static SubmissionRequestDto CreateValidSubmissionRequest(
        string? transactionNumber = null,
        int fileSize = 1024)
    {
        return new SubmissionRequestDto
        {
            TransactionNumber = transactionNumber ?? Guid.NewGuid().ToString("N").Substring(0, 12),
            FileContent = new byte[fileSize],
            FileName = "test_file.zip"
        };
    }

    public static QueryRequestDto CreateValidQueryRequest(
        string? year = null,
        string? ticketNumber = null)
    {
        return new QueryRequestDto
        {
            Year = year ?? DateTime.UtcNow.Year.ToString(),
            TicketNumber = ticketNumber ?? "0000000123"
        };
    }

    public static ReceiptDto CreateValidReceipt(
        string? ticketNumber = null,
        int errorCount = 0,
        int warningCount = 0)
    {
        var errors = new List<ErrorDetailDto>();
        for (int i = 0; i < errorCount; i++)
        {
            errors.Add(new ErrorDetailDto
            {
                Code = $"ERROR{i:D3}",
                Description = $"Error description {i}",
                Type = "Error"
            });
        }

        var warnings = new List<ErrorDetailDto>();
        for (int i = 0; i < warningCount; i++)
        {
            warnings.Add(new ErrorDetailDto
            {
                Code = $"WARN{i:D3}",
                Description = $"Warning description {i}",
                Type = "Warning"
            });
        }

        return new ReceiptDto
        {
            SubmissionYear = DateTime.UtcNow.Year.ToString(),
            IssuerDocument = "20100010136",
            ReceptionDate = DateTime.UtcNow,
            DocumentHash = "ABCD1234567890",
            OrderNumber = "00000001",
            TicketNumber = ticketNumber ?? "0000000123",
            Errors = errors,
            Warnings = warnings
        };
    }

    public static QueryResponseDto CreateValidQueryResponse(
        string? statusType = null,
        byte[]? responseData = null)
    {
        return new QueryResponseDto
        {
            Description = "Query completed successfully",
            StatusType = statusType ?? "ACEPTADO",
            ResponseData = responseData ?? new byte[256]
        };
    }
}
