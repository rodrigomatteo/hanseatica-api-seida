namespace Hanseatica.Api.Seida.Infrastructure.ExternalServices;

using Refit;

/// <summary>
/// Refit HTTP client interface for SEIDA external service
/// </summary>
[Headers("Content-Type: application/x-www-form-urlencoded")]
public interface ISeidaHttpClient
{
    [Post("/ReceptorService.htm")]
    Task<SeidaReceiptResponse> SubmitFileAsync(
        [Body(BodySerializationMethod.UrlEncoded)] SubmitFileRequest request,
        CancellationToken cancellationToken = default);

    [Post("/ReceptorService.htm")]
    Task<SeidaQueryResponse> QueryStatusAsync(
        [Body(BodySerializationMethod.UrlEncoded)] QueryStatusRequest request,
        CancellationToken cancellationToken = default);
}

public class SubmitFileRequest
{
    [AliasAs("usuario")]
    public string Username { get; set; } = string.Empty;

    [AliasAs("password")]
    public string EncryptedPassword { get; set; } = string.Empty;

    [AliasAs("numeroTransaccion")]
    public string TransactionNumber { get; set; } = string.Empty;

    [AliasAs("informacionArchivo")]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
}

public class QueryStatusRequest
{
    [AliasAs("usuario")]
    public string Username { get; set; } = string.Empty;

    [AliasAs("password")]
    public string EncryptedPassword { get; set; } = string.Empty;

    [AliasAs("parametrosConsulta")]
    public string QueryParameters { get; set; } = string.Empty;
}

public class SeidaReceiptResponse
{
    public string anhoEnvio { get; set; } = string.Empty;
    public string documentoEmisor { get; set; } = string.Empty;
    public DateTime fechaRecepcion { get; set; }
    public string hashDocumento { get; set; } = string.Empty;
    public string numeroOrden { get; set; } = string.Empty;
    public string ticketEnvio { get; set; } = string.Empty;
    public SeidaErrorDetail[] listaErrores { get; set; } = Array.Empty<SeidaErrorDetail>();
    public SeidaErrorDetail[] listaWarning { get; set; } = Array.Empty<SeidaErrorDetail>();
}

public class SeidaQueryResponse
{
    public string descripcion { get; set; } = string.Empty;
    public string tipo { get; set; } = string.Empty;
    public byte[] datos { get; set; } = Array.Empty<byte>();
}

public class SeidaErrorDetail
{
    public string codigo { get; set; } = string.Empty;
    public string descripcion { get; set; } = string.Empty;
    public string tipo { get; set; } = string.Empty;
}
