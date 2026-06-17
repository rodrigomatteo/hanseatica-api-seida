namespace Hanseatica.Api.Seida.Application.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; set; } = new();

    public ValidationException(string message) : base(message) { }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }

    public ValidationException(string fieldName, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { fieldName, new[] { message } }
        };
    }
}
