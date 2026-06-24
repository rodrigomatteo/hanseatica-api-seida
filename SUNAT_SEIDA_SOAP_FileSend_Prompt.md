# Prompt: Replicate SUNAT SEIDA SOAP File-Send Functionality

## Context

You need to replicate the `OnStartSend` method from a .NET Windows Forms client (`WinClienteSEIDA`) that sends ZIP files to SUNAT's **SEIDA** (Sistema de Envío Informatizado de Documentos Aduaneros) web service via SOAP. The service uses **WS-Security (WSS) UsernameToken** over the **WSE 3.0** stack.

---

## 1. Service Endpoints

| Environment | URL |
|---|---|
| **Production** | `https://www.sunat.gob.pe/ol-ad-itseida-ws/ReceptorService.htm` |
| **Testing** | `http://pruebaaduana.sunat.gob.pe/ol-ad-itseida-ws/ReceptorService.htm` |

- WSDL is served at the same URL with `?wsdl` appended.
- SOAP binding name: `ReceptorWebServiceServiceImplPortBinding`
- Service namespace: `http://service.receptor.tecnologia.sunat.gob.pe/`
- Request/Response body namespace: `http://services.sigad.sunat.gob.pe`

---

## 2. Credentials Required

There are **three distinct credential layers**:

### 2.1 SUNAT SOL Credentials (mandatory — injected into the SOAP Security header)

| Config key | Description | Example value |
|---|---|---|
| `NumeroRUC` | 11-digit taxpayer RUC number | `20100010136` |
| `UsuarioSOL` | SOL username (alphanumeric, SUNAT-assigned) | `PILOTONS` |
| `CodigoEnvio` | SOL access code / password (used as **PasswordText**) | `540D3D995F35` |

### 2.2 Username Composition (critical)

The `wsse:Username` value is **not** the raw `UsuarioSOL`. It is a composite string built by `Core.CombineUserData()`:

```
Username = NumeroRUC + UsuarioSOL + "|" + MAC_ADDRESS + "|" + IPv4_ADDRESS + "|2"
```

- `NumeroRUC` and `UsuarioSOL` are concatenated directly (no separator between them).
- `MAC_ADDRESS`: Ethernet adapter MAC formatted as `XX:XX:XX:XX:XX:XX` (uppercase hex octets, colon-separated), matched to the local IPv4 address.
- `IPv4_ADDRESS`: Local machine IPv4 address (first `InterNetwork` family address resolved via `Dns.GetHostAddresses(Dns.GetHostName())`).
- The trailing `|2` is a fixed literal suffix.

**Example:**
```
20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2
```

### 2.3 Optional Corporate Proxy Credentials

| Config key | Description |
|---|---|
| `ProxyActivo` | `"true"` to activate, `"false"` to skip |
| `ProxyDireccionIP` | Proxy server IP or hostname |
| `ProxyPuerto` | Proxy port (integer as string) |
| `ProxyNombreDominio` | Windows domain for NTLM/Kerberos auth |
| `ProxyUsuario` | Proxy username |
| `ProxyClave` | Proxy password |

---

## 3. SOAP Security Header Structure

The `CustomHeadersAssertion` / `ClientOutputFilter` (WSE 3.0 `SoapFilter`) **manually injects** a WS-Security header into every outgoing SOAP envelope inside `ProcessMessage()`. The resulting header looks like:

```xml
<SOAP-ENV:Header>
  <wsse:Security
	  xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"
	  soap:mustUnderstand="1">
	<wsse:UsernameToken
		xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
	  <wsse:Username>20100010136PILOTONS|88:51:FB:4A:50:26|192.168.1.45|2</wsse:Username>
	  <wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText">
		540D3D995F35
	  </wsse:Password>
	</wsse:UsernameToken>
  </wsse:Security>
</SOAP-ENV:Header>
```

**Key details:**
- Password type is `PasswordText` (plain text, no hashing).
- `mustUnderstand="1"` is set on the `Security` node.
- The `wsu` namespace is declared on the `UsernameToken` element itself.
- The header is appended via `envelope.Header.AppendChild(securityNode)` after `envelope.ImportNode(securityNode, true)`.

---

## 4. The `recibirArchivo` SOAP Operation (file send)

```
SOAPAction: urn:recibirArchivo
Style:      Document/Literal Wrapped
```

### Request Parameters

| Parameter | XML local name | Type | Description |
|---|---|---|---|
| `numeroTransaccion` | `numeroTransaccion` | `xsd:string` | Business transaction identifier (min 4 chars) |
| `informacionArchivo` | `informacionArchivo` | `xsd:base64Binary` | Full content of the ZIP file as a byte array |

### Response — `acuseRecibo` Object

| Field | Type | Description |
|---|---|---|
| `anhoEnvio` | string | Year of the submission |
| `ticketEnvio` | string | SUNAT-assigned ticket number |
| `documentoEmisor` | string | Emitter document |
| `fechaRecepcion` | string | Reception timestamp |
| `hashDocumento` | string | Document hash |
| `numeroOrden` | string | Order number |
| `listaErrores` | `errorAcuse[]` | Validation errors (nullable) |
| `listaWarning` | `errorAcuse[]` | Validation warnings (nullable) |

Each `errorAcuse` has: `codigo` (string), `descripcion` (string), `tipo` (string).

---

## 5. Step-by-Step Call Sequence

```
1. Read file → byte[] fileArray = File.ReadAllBytes(zipFilePath)

2. Create proxy client pointing to the endpoint URL
   (extends WebServicesClientProtocol / HttpWebClientProtocol)

3. If ProxyActivo == "true":
	 Create NetworkCredential(proxyUser, proxyClave, proxyDomain)
	 Create WebProxy(proxyIP, proxyPort)  →  assign credentials  →  assign to proxy client

4. Build WSS UsernameToken:
	 username = CombineUserData(NumeroRUC, UsuarioSOL)
				= NumeroRUC + UsuarioSOL + "|" + MAC + "|" + IP + "|2"
	 password = CodigoEnvio

5. Inject wsse:Security header into every outgoing envelope
   (via WSE 3.0 Policy+PolicyAssertion+SoapFilter OR manual HttpClient header injection)

6. Call recibirArchivo(numeroTransaccion, fileArray)
	 → async: BeginrecibirArchivo / EndrecibirArchivo
	 → sync:  recibirArchivo(...)

7. Cast result to acuseRecibo

8. Serialize acuseRecibo to XML:
	 filename = "seida_acuse_" + acuse.anhoEnvio + "_" + acuse.ticketEnvio.Trim() + ".xml"
	 XmlSerializer.Serialize(StreamWriter, acuseRecibo)

9. Error handling: parse SOAP fault body for <faultcode>/<faultstring>
   (fault code 91500 = wrong username/password)
```

---

## 6. The `realizarConsulta` SOAP Operation (ticket query)

```
SOAPAction: urn:realizarConsulta
```

### Request

An XML string literal passed as the `parametrosConsulta` parameter:

```xml
<consulta>
  <tipo>1</tipo>
  <parametros>
	<numeroTicket>TICKET_VALUE</numeroTicket>
	<annoTicket>YEAR_VALUE</annoTicket>
  </parametros>
</consulta>
```

### Response

- `respuestaConsultaBean` (fields: `descripcion`, `tipo`)
- `out byte[] realizarConsultaResultado` — the raw response file content; save as:

```
seida_respuesta_{anno}_{yyyyMMddHHmmss}_{ticket}.xml
```

---

## 7. Key Implementation Notes for the New Project

1. **WSE 3.0 dependency**: The original uses `Microsoft.Web.Services3.dll`. If the new project cannot use this legacy library, replicate the header injection manually using `HttpClient` + a custom `DelegatingHandler`, or a modern WCF `IClientMessageInspector`.

2. **Username is machine-dependent**: The composite username includes the client machine's MAC and IP. If running server-side or in a container, replace with a fixed/configured MAC and IP, or confirm with SUNAT whether a simplified format is accepted.

3. **Password is plain text**: `PasswordText` — do **not** hash or encode the `CodigoEnvio`.

4. **No WS-Addressing**: The `CustomHeadersAssertion.GetExtensions()` registers itself as `RemoveAddressingHeadersAssertion`, meaning WS-Addressing headers (added by WSE 3.0 by default) are suppressed. When replicating with modern stacks, ensure no `wsa:` headers are added.

5. **Transport**: Standard HTTP/HTTPS. The SOAP body is `Document/Literal/Wrapped` style.

6. **File format**: Input must be a `.zip` file. The byte array is transmitted as `xsd:base64Binary`.

7. **Error parsing**: On authentication failure or business error, SUNAT returns a SOAP `<S:Fault>` with `<faultcode>` (e.g., `91500`) and `<faultstring>` inside the body. Parse between those tags to extract the human-readable error.
