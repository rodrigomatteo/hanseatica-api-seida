namespace Hanseatica.Api.Seida.Infrastructure.ExternalServices;

using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

public interface ISeidaHttpClient
{
    Task<SeidaReceiptResponse> SubmitFileAsync(SubmitFileRequest request, CancellationToken cancellationToken = default);
    Task<SeidaQueryResponse> QueryStatusAsync(QueryStatusRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// SOAP HTTP client for the SUNAT SEIDA external service.
/// Endpoint: https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/ReceptorService.htm
/// WSDL:     https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/ReceptorService.htm?wsdl
/// </summary>
public class SeidaHttpClient : ISeidaHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SeidaHttpClient> _logger;

    private static readonly XNamespace SoapEnvNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace ServiceNs = "http://services.sigad.sunat.gob.pe";
    private static readonly XNamespace WsseNs    = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace WsuNs     = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    public SeidaHttpClient(HttpClient httpClient, ILogger<SeidaHttpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SeidaReceiptResponse> SubmitFileAsync(
        SubmitFileRequest request, CancellationToken cancellationToken = default)
    {
        /* Raw XML reference (now generated dynamically):
        var envelopeXml = $@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:wsa=""http://schemas.xmlsoap.org/ws/2004/08/addressing"">
        <soap:Header>
          <wsa:Action>urn:recibirArchivo</wsa:Action>
          <wsa:MessageID>urn:uuid:a8b25779-b28c-4e1c-8e6a-fb1d86b06b01</wsa:MessageID>
          <wsa:ReplyTo>
            <wsa:Address>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</wsa:Address>
          </wsa:ReplyTo>
          <wsa:To>https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/ReceptorService.htm?wsdl</wsa:To>
          <wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
            <wsse:UsernameToken xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
              <wsse:Username>20100118336TESTPAS1|00:15:5D:60:7B:00|169.254.123.253|2</wsse:Username>
              <wsse:Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText"">TESTPAS1</wsse:Password>
            </wsse:UsernameToken>
          </wsse:Security>
        </soap:Header>
        <soap:Body>
          <recibirArchivo xmlns=""http://services.sigad.sunat.gob.pe"">
            <numeroTransaccion xmlns="""">0000</numeroTransaccion>
            <informacionArchivo xmlns="""">UEsDBBQAAAAIAMpWxVzQCD3GKAQAAP4MAAAmAAAAMDFfMDQ3MV8yMDI2XzYxMDExMl8yMDI2MDYwNTEwNTQyMC5YTUy9VluTokYUfk9V/kMv7yMXHXWsli0W2FkSvCxgMqmtra1ebF0qSlsNxvHf59AgAoKZ5CEPVjXnfr7+zmnx+9f9Dv1FeRKxeCqpPUVCNA7ZOoq3UylK2MN4/Pj0oErovf7zT9ii4Y5wkoLxjKbEIilBr0k0ScIfdE9cFgrVVDryeHIK2WQNBnu2prvJ0p5YLDzuaZxePCcqaon3bWl/Uw9K7zVZSwiKi5MJZJhKP9L0MJHl0+nUO/V7jG9lTVFU+WXm+iL5QxQnKYlDWni9uYhLlnWXS1mZ5YN1BsPv5iITzDKL33LsTDjr/d4Iy13K3C84H+ic7Klu2aawLQWg92hyYHESfd9Rkx3jlJ+F59LGcoeq7mRs4e7OIpq/mhtBza2iBK+PxzjMYCc7i26iOMrOujIYqVhuVdUv/z7EV7s3oFuxzgEuHOibPOzLnSCEyp4EMvVeCrCqVmTn0Q3lAAp1LF1TtOFQVVRVqwJQtci9nSQ5UrhfGkT7ImImXieTi9BPOQwP2jC+J2mWdyppykASGZSh8qg8qQNNwfKNS5FBbkmBM5aIHsyVbxoelktBri+IBmXCCGP5+lm4s5TsnjlLkhmBHyXJkVN0hLvNC/z1eSbp6khReooC7q3m1VBLEv5JtvTzkcRplJ51Vbt4NTW5k3/8vo/SlPIrYo6FxNYAaKeSgEeFeVbH/f4QILCuhh7b5Z1mOcqPUm2yOCVhWgoymSA5BByMRv3xeNjHckH7q0mNGB+cNqaIy6iFx3KjkepMLDabKKTVBnWt/3jtBcsdxthYr6Ni4IrtVIvyyZgbtqKNx091YNqp+BY2ajkbOznYRcNMHmXiy6a/9NgUltYlTfuZWYO0clfj+APja8oDTmJYXxyWNRyuQbPVKsIMsFyeS61FD4SnQNj/BM11UGEaFDEN90DqToZ/YbC96BnQgE0ElV6/i+67m8Qm4TyqTovgr+F4M+MF+T2j12B0yzTBnhmp48G43zFN2aKrTxOQvZo2m6wk2sZ1Pt7h6l1O/jvsH/8Z+zscrRFvpNwQ7y73Kp3TekiBOH73xbSMwPjiB8bcMjwLebbp2UbgLOZosbQ9OM2fETIXs6Ux/wO5rvn1q96ygErszXnbWpNva6heiJPSfS2cyfbwQJYb96qwaBLy6CAe8bL4wH4JHNf2kbFcGp7tihKrlo0gJkC8ZfwMa30XbYAi+drMGNOmqN3RbWVlczedXFpkvM6qG3YP4akAfmdPfIXdNVz9j/dwrWTAz4ytk8v2aiTWV/7MMW6ylIQatrOrPSR2Gcn+WXflWtquM6vPq9zlgucsjTbnJayf8/9B07nfAifKNwKnSdKkjBvFlTJUDbn2C6QMIL+5WHkB8j/ZrvgKlOFgPEAr3xAlCMcag5oZoMxm7zVKZX9Wq49t8/vyh17/G1BLAQIAABQAAAAIAMpWxVzQCD3GKAQAAP4MAAAmAAAAAAAAAAAAIAAAAAAAAAAwMV8wNDcxXzIwMjZfNjEwMTEyXzIwMjYwNjA1MTA1NDIwLlhNTFBLBQYAAAAAAQABAFQAAABsBAAAAAA=</informacionArchivo>
          </recibirArchivo>
        </soap:Body>
      </soap:Envelope>";
        */

        var wsaNs = XNamespace.Get("http://schemas.xmlsoap.org/ws/2004/08/addressing");
        var securityHeader = new XElement(WsseNs + "Security",
            new XAttribute(SoapEnvNs + "mustUnderstand", "1"),
            new XElement(WsseNs + "UsernameToken",
                new XAttribute(XNamespace.Xmlns + "wsu", WsuNs.NamespaceName),
                new XElement(WsseNs + "Username", request.Username),
                new XElement(WsseNs + "Password",
                    new XAttribute("Type", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText"),
                    request.Password)));

        var headerContent = new XElement(SoapEnvNs + "Header",
            new XElement(wsaNs + "Action", "urn:recibirArchivo"),
            new XElement(wsaNs + "MessageID", $"urn:uuid:{Guid.NewGuid()}"),
            new XElement(wsaNs + "ReplyTo",
                new XElement(wsaNs + "Address", "http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous")),
            new XElement(wsaNs + "To", "https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/ReceptorService.htm?wsdl"),
            securityHeader);

        var body = new XElement(ServiceNs + "recibirArchivo",
            new XElement(XNamespace.None + "numeroTransaccion", request.TransactionNumber),
            new XElement(XNamespace.None + "informacionArchivo", request.FileContent));

        var envelope = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(SoapEnvNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", SoapEnvNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                new XAttribute(XNamespace.Xmlns + "wsa", wsaNs.NamespaceName),
                headerContent,
                new XElement(SoapEnvNs + "Body", body)));

        var doc = await PostSoapAsync(envelope, "recibirArchivo", cancellationToken);
        return ParseSubmitResponse(doc);
    }

    public async Task<SeidaQueryResponse> QueryStatusAsync(
        QueryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var securityHeader = new XElement(WsseNs + "Security",
            new XAttribute(SoapEnvNs + "mustUnderstand", "1"),
            new XElement(WsseNs + "UsernameToken",
                new XAttribute(XNamespace.Xmlns + "wsu", WsuNs.NamespaceName),
                new XElement(WsseNs + "Username", request.Username),
                new XElement(WsseNs + "Password",
                    new XAttribute("Type", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText"),
                    request.Password)));

        var envelope = BuildEnvelope(ServiceNs + "realizarConsulta",
            securityHeader,
            new XElement(ServiceNs + "parametrosConsulta", request.QueryParameters));

        var doc = await PostSoapAsync(envelope, "realizarConsulta", cancellationToken);
        return ParseQueryResponse(doc);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? GetElementValue(XElement parent, string localName)
        => parent.Element(localName)?.Value  // Try no namespace first
           ?? parent.Element(ServiceNs + localName)?.Value;  // Try with ServiceNs

    private static XDocument BuildEnvelope(XName operation, XElement? headerContent, params XElement[] bodyChildren)
        => new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(SoapEnvNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", SoapEnvNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "wsse", WsseNs.NamespaceName),
                new XElement(SoapEnvNs + "Header", (object?)headerContent),
                new XElement(SoapEnvNs + "Body",
                    new XElement(operation,
                        new XAttribute("xmlns", ServiceNs.NamespaceName),
                        bodyChildren))));

    private async Task<XDocument> PostSoapAsync(
        XDocument envelope, string operation, CancellationToken cancellationToken)
    {
        var content = new StringContent(envelope.ToString(), Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"\"urn:{operation}\"");

        _logger.LogDebug("SOAP request [{Operation}]:\n{Envelope}", operation, envelope.ToString());

        var response = await _httpClient.PostAsync("ReceptorService.htm", content, cancellationToken);

        // Read response body first to capture error details
        var xml = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Include the actual SOAP response for debugging
            throw new HttpRequestException(
                $"SOAP request failed with status {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Response: {xml}");
        }

        return XDocument.Parse(xml);
    }

    private static SeidaReceiptResponse ParseSubmitResponse(XDocument doc)
    {
        // Try new structure first (recibirArchivoResultado), fall back to old (return)
        var ret = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "recibirArchivoResultado")
                   ?? GetReturnElement(doc);

        var result = new SeidaReceiptResponse
        {
            anhoEnvio       = GetElementValue(ret, "anhoEnvio")       ?? string.Empty,
            documentoEmisor = GetElementValue(ret, "documentoEmisor") ?? string.Empty,
            hashDocumento   = GetElementValue(ret, "hashDocumento")   ?? string.Empty,
            numeroOrden     = GetElementValue(ret, "numeroOrden")     ?? string.Empty,
            ticketEnvio     = GetElementValue(ret, "ticketEnvio")     ?? string.Empty,
            listaErrores    = ParseErrorList(ret, "listaErrores"),
            listaWarning    = ParseErrorList(ret, "listaWarning"),
        };

        var fechaValue = GetElementValue(ret, "fechaRecepcion");
        if (DateTime.TryParse(fechaValue, out var date))
            result.fechaRecepcion = date;

        return result;
    }

    private static SeidaQueryResponse ParseQueryResponse(XDocument doc)
    {
        var ret      = GetReturnElement(doc);
        var base64   = ret.Element(ServiceNs + "datos")?.Value;
        return new SeidaQueryResponse
        {
            descripcion = ret.Element(ServiceNs + "descripcion")?.Value ?? string.Empty,
            tipo        = ret.Element(ServiceNs + "tipo")?.Value        ?? string.Empty,
            datos       = string.IsNullOrEmpty(base64)
                              ? Array.Empty<byte>()
                              : Convert.FromBase64String(base64)
        };
    }

    private static XElement GetReturnElement(XDocument doc)
        => doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "return")
           ?? throw new InvalidOperationException(
               "Invalid SOAP response: <return> element not found. Check for a SOAP Fault in the response.");

    private static SeidaErrorDetail[] ParseErrorList(XElement parent, string elementName)
    {
        var elements = parent.Elements(elementName).Concat(parent.Elements(ServiceNs + elementName));

        return elements.Select(e => new SeidaErrorDetail
        {
            codigo      = GetElementValue(e, "codigo")      ?? string.Empty,
            descripcion = GetElementValue(e, "descripcion") ?? string.Empty,
            tipo        = GetElementValue(e, "tipo")        ?? string.Empty
        })
        .ToArray();
    }
}

public class SubmitFileRequest
{
    public string Username         { get; set; } = string.Empty;
    public string Password         { get; set; } = string.Empty;
    public string TransactionNumber { get; set; } = string.Empty;
    public string FileContent      { get; set; } = string.Empty;
}

public class QueryStatusRequest
{
    public string Username        { get; set; } = string.Empty;
    public string Password        { get; set; } = string.Empty;
    public string QueryParameters { get; set; } = string.Empty;
}

public class SeidaReceiptResponse
{
    public string anhoEnvio         { get; set; } = string.Empty;
    public string documentoEmisor   { get; set; } = string.Empty;
    public DateTime fechaRecepcion  { get; set; }
    public string hashDocumento     { get; set; } = string.Empty;
    public string numeroOrden       { get; set; } = string.Empty;
    public string ticketEnvio       { get; set; } = string.Empty;
    public SeidaErrorDetail[] listaErrores { get; set; } = Array.Empty<SeidaErrorDetail>();
    public SeidaErrorDetail[] listaWarning { get; set; } = Array.Empty<SeidaErrorDetail>();
}

public class SeidaQueryResponse
{
    public string descripcion { get; set; } = string.Empty;
    public string tipo        { get; set; } = string.Empty;
    public byte[] datos       { get; set; } = Array.Empty<byte>();
}

public class SeidaErrorDetail
{
    public string codigo      { get; set; } = string.Empty;
    public string descripcion { get; set; } = string.Empty;
    public string tipo        { get; set; } = string.Empty;
}
