namespace Hanseatica.Api.Seida.Tests.Unit.ExternalServices;

using System.Xml.Linq;
using FluentAssertions;
using Hanseatica.Api.Seida.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Tests that validate the SOAP request structure matches SUNAT's specification.
/// Based on: https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/ReceptorService.htm?wsdl
/// </summary>
public class SoapRequestStructureTests
{
    private static readonly XNamespace SoapEnvNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace WsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace WsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private static readonly XNamespace ServiceNs = "http://services.sigad.sunat.gob.pe";

    private class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; set; }
        public XDocument? CapturedXml { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            var content = await request.Content!.ReadAsStringAsync(cancellationToken);
            CapturedXml = XDocument.Parse(content);

            return new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(CreateValidSeidaResponse())
            };
        }
    }

    [Fact]
    public async Task SubmitFileAsync_GeneratesValidSoapEnvelopeStructure()
    {
        // Arrange
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/")
        };
        var seidaClient = new SeidaHttpClient(httpClient, NullLogger<SeidaHttpClient>.Instance);

        var request = new SubmitFileRequest
        {
            Username = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2",
            Password = "540D3D995F35",
            TransactionNumber = "TXN-2024-001234",
            FileContent = Convert.ToBase64String(new byte[] { 0x50, 0x4B, 0x03, 0x04 })
        };

        // Act
        await seidaClient.SubmitFileAsync(request);

        // Assert - Envelope structure
        handler.CapturedXml.Should().NotBeNull();
        var root = handler.CapturedXml!.Root;
        root!.Name.Should().Be(SoapEnvNs + "Envelope");

        // Assert - Envelope namespace declarations
        root.Attribute(XNamespace.Xmlns + "soap")?.Value.Should().Be(SoapEnvNs.NamespaceName);
        root.Attribute(XNamespace.Xmlns + "wsse")?.Value.Should().Be(WsseNs.NamespaceName);
    }

    [Fact]
    public async Task SubmitFileAsync_GeneratesValidWsSecurityHeader()
    {
        // Arrange
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/")
        };
        var seidaClient = new SeidaHttpClient(httpClient, NullLogger<SeidaHttpClient>.Instance);

        var compositeUsername = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2";
        var password = "540D3D995F35";

        var request = new SubmitFileRequest
        {
            Username = compositeUsername,
            Password = password,
            TransactionNumber = "TXN-2024-001234",
            FileContent = "UEsDBBQ="
        };

        // Act
        await seidaClient.SubmitFileAsync(request);

        // Assert - Security header exists
        var header = handler.CapturedXml!.Root?.Element(SoapEnvNs + "Header");
        header.Should().NotBeNull();

        var security = header!.Element(WsseNs + "Security");
        security.Should().NotBeNull();
        security!.Attribute(SoapEnvNs + "mustUnderstand")?.Value.Should().Be("1");

        // Assert - UsernameToken structure
        var usernameToken = security.Element(WsseNs + "UsernameToken");
        usernameToken.Should().NotBeNull();
        usernameToken!.Attribute(XNamespace.Xmlns + "wsu")?.Value.Should().Be(WsuNs.NamespaceName);

        // Assert - Credentials in token
        usernameToken.Element(WsseNs + "Username")?.Value.Should().Be(compositeUsername);

        var passwordElement = usernameToken.Element(WsseNs + "Password");
        passwordElement.Should().NotBeNull();
        passwordElement!.Value.Should().Be(password);
        passwordElement.Attribute("Type")?.Value.Should().Be(
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText");
    }

    [Fact]
    public async Task SubmitFileAsync_BodyOperationUsesDefaultNamespace()
    {
        // Arrange
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/")
        };
        var seidaClient = new SeidaHttpClient(httpClient, NullLogger<SeidaHttpClient>.Instance);

        var request = new SubmitFileRequest
        {
            Username = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2",
            Password = "540D3D995F35",
            TransactionNumber = "TXN-2024-001234",
            FileContent = "UEsDBBQ="
        };

        // Act
        await seidaClient.SubmitFileAsync(request);

        // Assert - Body structure
        var body = handler.CapturedXml!.Root?.Element(SoapEnvNs + "Body");
        body.Should().NotBeNull();

        var operation = body!.Element(ServiceNs + "recibirArchivo");
        operation.Should().NotBeNull();
        operation!.Attribute("xmlns")?.Value.Should().Be(ServiceNs.NamespaceName);

        // Assert - Child elements use default namespace (no prefix)
        var numeroTransaccion = operation.Element(ServiceNs + "numeroTransaccion");
        numeroTransaccion.Should().NotBeNull();
        numeroTransaccion!.Value.Should().Be("TXN-2024-001234");

        var informacionArchivo = operation.Element(ServiceNs + "informacionArchivo");
        informacionArchivo.Should().NotBeNull();
        informacionArchivo!.Value.Should().Be("UEsDBBQ=");
    }

    [Fact]
    public async Task SubmitFileAsync_NoCredentialsInSoapBody()
    {
        // Arrange
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/")
        };
        var seidaClient = new SeidaHttpClient(httpClient, NullLogger<SeidaHttpClient>.Instance);

        var request = new SubmitFileRequest
        {
            Username = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2",
            Password = "540D3D995F35",
            TransactionNumber = "TXN-2024-001234",
            FileContent = "UEsDBBQ="
        };

        // Act
        await seidaClient.SubmitFileAsync(request);

        // Assert - Credentials NOT in body
        var body = handler.CapturedXml!.Root?.Element(SoapEnvNs + "Body");
        var operation = body!.Element(ServiceNs + "recibirArchivo");

        operation!.Elements().Should().NotContain(e => e.Name.LocalName == "usuario");
        operation!.Elements().Should().NotContain(e => e.Name.LocalName == "password");
        operation!.Elements().Should().NotContain(e => e.Name.LocalName == "EncryptedPassword");
    }

    [Fact]
    public async Task QueryStatusAsync_GeneratesValidSoapEnvelopeForConsulta()
    {
        // Arrange
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/")
        };
        var seidaClient = new SeidaHttpClient(httpClient, NullLogger<SeidaHttpClient>.Instance);

        var request = new QueryStatusRequest
        {
            Username = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2",
            Password = "540D3D995F35",
            QueryParameters = """<consulta><tipo>1</tipo><parametros><numeroTicket>0000000001</numeroTicket><annoTicket>2024</annoTicket></parametros></consulta>"""
        };

        // Act & Assert
        try
        {
            await seidaClient.QueryStatusAsync(request);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("return"))
        {
            // Expected - mock doesn't have real response
        }

        // Verify the request was captured and has correct structure
        handler.CapturedXml.Should().NotBeNull();
        var operation = handler.CapturedXml!.Root?.Element(SoapEnvNs + "Body")
            ?.Element(ServiceNs + "realizarConsulta");
        operation.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitFileAsync_ContentTypeIsTextXml()
    {
        // Arrange
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/")
        };
        var seidaClient = new SeidaHttpClient(httpClient, NullLogger<SeidaHttpClient>.Instance);

        var request = new SubmitFileRequest
        {
            Username = "20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2",
            Password = "540D3D995F35",
            TransactionNumber = "TXN-2024-001234",
            FileContent = "UEsDBBQ="
        };

        // Act
        await seidaClient.SubmitFileAsync(request);

        // Assert - Content-Type header
        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.Content?.Headers.ContentType?.MediaType.Should().Be("text/xml");
        handler.CapturedRequest.Content?.Headers.ContentType?.CharSet.Should().Be("utf-8");
    }

    [Fact]
    public async Task SubmitFileAsync_CompositeUsernameFormatIsCorrect()
    {
        // Arrange
        var handler = new CaptureHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://e-seida.sunat.gob.pe/ol-ad-itseida-ws/")
        };
        var seidaClient = new SeidaHttpClient(httpClient, NullLogger<SeidaHttpClient>.Instance);

        var numeroRuc = "20100010136";
        var usuarioSOL = "PILOTONS";
        var expectedUsername = $"{numeroRuc}{usuarioSOL}|88:51:FB:4A:50:26|192.168.1.45|2";

        var request = new SubmitFileRequest
        {
            Username = expectedUsername,
            Password = "540D3D995F35",
            TransactionNumber = "TXN-2024-001234",
            FileContent = "UEsDBBQ="
        };

        // Act
        await seidaClient.SubmitFileAsync(request);

        // Assert - Username has exact format: RUC+SOL|MAC|IP|2
        var usernameElement = handler.CapturedXml!.Descendants(WsseNs + "Username").FirstOrDefault();
        usernameElement.Should().NotBeNull();
        usernameElement!.Value.Should().Be(expectedUsername);
        usernameElement.Value.Should().Match("*|88:51:FB:4A:50:26|192.168.1.45|2");
    }

    private static string CreateValidSeidaResponse()
    {
        return """
            <?xml version="1.0" encoding="UTF-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <ns2:recibirArchivoResponse xmlns:ns2="http://services.sigad.sunat.gob.pe">
                  <return>
                    <anhoEnvio>2024</anhoEnvio>
                    <ticketEnvio>0000000001</ticketEnvio>
                    <documentoEmisor>20100010136</documentoEmisor>
                    <fechaRecepcion>2024-06-17T15:30:00</fechaRecepcion>
                    <hashDocumento>HASH123</hashDocumento>
                    <numeroOrden>001</numeroOrden>
                  </return>
                </ns2:recibirArchivoResponse>
              </soap:Body>
            </soap:Envelope>
            """;
    }

    private static string CreateValidSeidaQueryResponse()
    {
        return """
            <?xml version="1.0" encoding="UTF-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <ns2:realizarConsultaResponse xmlns:ns2="http://services.sigad.sunat.gob.pe">
                  <return>
                    <descripcion>OK</descripcion>
                    <tipo>0</tipo>
                    <datos>UEsDBBQ=</datos>
                  </return>
                </ns2:realizarConsultaResponse>
              </soap:Body>
            </soap:Envelope>
            """;
    }
}

