namespace Hanseatica.Api.Seida.Application.Exceptions;

/// <summary>
/// Exception thrown when SEIDA service operations fail
/// </summary>
public class SeidaServiceException : Exception
{
    public string ErrorCode { get; set; } = string.Empty;

    public SeidaServiceException(string message) : base(message) { }

    public SeidaServiceException(string message, Exception innerException)
        : base(message, innerException) { }

    public SeidaServiceException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public SeidaServiceException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
