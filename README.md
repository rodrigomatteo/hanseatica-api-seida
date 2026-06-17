# Hanseatica.Api.Seida - SEIDA Web API

A modern, production-ready .NET 10 Web API for SUNAT's SEIDA (Sistema de Envío de Información de Aduanas / Customs Information Sending System). This solution migrates the logic from the legacy Windows Forms application to a clean, scalable microservice architecture.

## Overview

SEIDA is Peru's customs information submission system operated by SUNAT (National Tax Authority). This API provides:

- **File Submission**: Send digitally signed documents to SEIDA
- **Status Queries**: Check submission status using ticket numbers
- **Error Handling**: Comprehensive error reporting from SEIDA service
- **Health Checks**: System and external service health monitoring
- **API Documentation**: Interactive Swagger/OpenAPI interface

## Architecture

This solution implements **Clean Architecture** principles with clear separation of concerns:

```
Domain Layer (Entities, Value Objects, Enums)
    ↑
Application Layer (Use Cases, DTOs, Interfaces, Services)
    ↑
Infrastructure Layer (External Services, Implementations, Refit Client)
    ↑
API Layer (Controllers, Configuration, Health Checks)
```

### Project Structure

- **`Hanseatica.Api.Seida.Domain`**: Core business entities and value objects
  - Entities: `Submission`, `Receipt`, `Query`, `QueryResponse`
  - Value Objects: `SecurityCredentials`, `FileInfo`
  - Enums: `QueryStatusType`

- **`Hanseatica.Api.Seida.Application`**: Business logic and orchestration
  - Services: `SubmissionService`, `QueryService`
  - DTOs: Request/Response data transfer objects
  - Interfaces: Service contracts and external client interface
  - Exceptions: Custom exception types

- **`Hanseatica.Api.Seida.Infrastructure`**: External integrations and implementations
  - `SeidaClient`: Implements ISeidaClient, maps to DTOs
  - `ISeidaHttpClient`: Refit-based HTTP client for SEIDA service
  - `FileService`: File I/O operations
  - `AuthenticationService`: Credential management and AES encryption
  - `NetworkService`: IP/MAC address retrieval

- **`Hanseatica.Api.Seida.Api`**: ASP.NET Core Web API
  - Controllers: REST endpoints for submissions and queries
  - Dependency Injection: Service registration and configuration
  - Health Checks: System and external service health endpoints
  - Swagger: API documentation and testing interface

- **`Hanseatica.Api.Seida.Tests`**: Comprehensive unit tests
  - Service tests (80%+ coverage)
  - Controller tests
  - Domain entity tests
  - Test fixtures and data builders

## API Endpoints

### Submissions

#### POST `/api/submissions/submit`
Submit a file with embedded content.

**Request Body:**
```json
{
  "transactionNumber": "20240614120000ABCD",
  "fileContent": "base64-encoded-zip",
  "fileName": "document.zip"
}
```

**Response:**
```json
{
  "submissionYear": "2024",
  "issuerDocument": "20100010136",
  "receptionDate": "2024-06-14T12:00:00Z",
  "documentHash": "ABCD1234567890",
  "orderNumber": "00000001",
  "ticketNumber": "0000000123",
  "errors": [
    {
      "code": "ERR001",
      "description": "Error description",
      "type": "Error"
    }
  ],
  "warnings": [],
  "hasErrors": false,
  "hasWarnings": false
}
```

#### POST `/api/submissions/submit-with-transaction`
Submit a file as multipart form data with custom transaction number.

**Parameters:**
- `transactionNumber` (query): Custom transaction ID
- `file` (form): ZIP file to submit

**Response:** Same as above

#### GET `/api/submissions/generate-transaction-id`
Generate a unique transaction number.

**Response:**
```json
{
  "transactionId": "20240614120000ABCD1234"
}
```

### Queries

#### POST `/api/queries/query-status`
Query submission status using JSON body.

**Request Body:**
```json
{
  "year": "2024",
  "ticketNumber": "0000000123"
}
```

**Response:**
```json
{
  "description": "Query completed successfully",
  "statusType": "ACEPTADO",
  "responseData": "base64-encoded-response"
}
```

#### GET `/api/queries/query-status`
Query submission status using URL parameters.

**Parameters:**
- `year` (query): Submission year (e.g., "2024")
- `ticketNumber` (query): Ticket number (e.g., "0000000123")

**Response:** Same as above

## Configuration

All external service URLs and credentials are configured in `appsettings.json`:

### Production Settings (`appsettings.json`)

```json
{
  "Seida": {
    "BaseUrl": "https://www.sunat.gob.pe/ol-ad-itseida-ws",
    "Username": "",
    "Password": "",
    "TimeoutSeconds": 30,
    "ProxyEnabled": false,
    "ProxyAddress": "",
    "ProxyPort": 0,
    "ProxyUsername": "",
    "ProxyPassword": ""
  },
  "ExternalServices": {
    "SeidaBaseUrl": "https://www.sunat.gob.pe/ol-ad-itseida-ws"
  }
}
```

### Development Settings (`appsettings.Development.json`)

```json
{
  "Seida": {
    "BaseUrl": "http://pruebaaduana.sunat.gob.pe/ol-ad-itseida-ws",
    "Username": "PILOTONS",
    "Password": "change_me",
    "TimeoutSeconds": 30
  },
  "ExternalServices": {
    "SeidaBaseUrl": "http://pruebaaduana.sunat.gob.pe/ol-ad-itseida-ws"
  }
}
```

**Environment Variables:**

Set sensitive values via environment variables instead of configuration files:

```bash
export Seida__Username=your_username
export Seida__Password=your_password
export ExternalServices__SeidaBaseUrl=https://your.seida.endpoint
```

## Health Checks

The API includes health checks for system and external services:

### Endpoints

- `GET /health` - Overall health status
- `GET /health/ready` - Readiness probe (checks external dependencies)

### Example Response

```json
{
  "status": "Healthy",
  "checks": {
    "disk": {
      "status": "Healthy"
    },
    "seida-service": {
      "status": "Healthy",
      "description": "SEIDA external service is responding"
    }
  }
}
```

## Building and Running

### Prerequisites

- .NET 10 SDK
- Windows or Linux/macOS with .NET runtime

### Build

```bash
cd Hanseatica.Api.Seida
dotnet build
```

### Run

```bash
cd Hanseatica.Api.Seida.Api
dotnet run
```

The API will start on `http://localhost:5000` with Swagger available at `http://localhost:5000/swagger`

### Run in Development Environment

```bash
dotnet run --environment Development
```

## Testing

The solution includes comprehensive unit tests with 80%+ code coverage using xUnit, Moq, and FluentAssertions.

### Run All Tests

```bash
dotnet test
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Projects

- **SubmissionServiceTests**: File submission business logic (9 tests)
- **QueryServiceTests**: Status query business logic (7 tests)
- **FileServiceTests**: File I/O operations (8 tests)
- **AuthenticationServiceTests**: Credential management and encryption (8 tests)
- **SeidaClientTests**: External service integration (6 tests)
- **SubmissionsControllerTests**: File submission endpoints (7 tests)
- **QueriesControllerTests**: Query endpoints (7 tests)
- **EntitiesTests**: Domain model validation (15 tests)

**Total: 67 unit tests**

## Key Features Implemented

### 1. Clean Code Architecture
- Clear separation of concerns
- Dependency injection throughout
- Interface-driven design
- Domain-driven entities

### 2. Refit Integration
- Type-safe HTTP client
- Automatic request/response serialization
- Built-in resilience handlers

### 3. AES Encryption
- Credential encryption/decryption
- RFC2898 key derivation
- Secure authentication header generation

### 4. Error Handling
- Custom exception types (`SeidaServiceException`, `ValidationException`)
- SOAP fault parsing
- Comprehensive error details in responses

### 5. Logging
- Structured logging with Microsoft.Extensions.Logging
- Debug-level logs in development
- Information-level logs in production

### 6. Configuration Management
- Environment-specific settings
- Environment variable support
- No hardcoded URLs or credentials

### 7. API Documentation
- Swagger/OpenAPI integration
- XML comment documentation
- Interactive API testing interface

### 8. Health Checks
- System disk space monitoring
- External service availability checks
- Customizable health check endpoints

## Migration from Windows Forms

This solution migrates the following logic from the original WinClienteSEIDA application:

| Feature | Legacy | New |
|---------|--------|-----|
| File Submission | `ThreadProxy.OnStartSend()` | `SubmissionService.SubmitFileAsync()` |
| Status Query | `ThreadProxy.OnStartQuery()` | `QueryService.QueryStatusAsync()` |
| Error Parsing | `Core.EvaluateMessageError()` | `SeidaClient` mapping logic |
| Encryption | `Core.Encrypt/Decrypt()` | `AuthenticationService` |
| File Operations | `Core.Generate*FileName()` | `FileService` |
| Authentication | `CustomHeadersAssertion` | WSE3-compatible headers via Refit |
| Proxy Support | Manual `WebProxy` setup | Configuration-based setup |

## Dependencies

### NuGet Packages

- **Refit** (`Refit`, `Refit.HttpClientFactory`): Type-safe HTTP client
- **Swashbuckle.AspNetCore**: Swagger/OpenAPI documentation
- **AspNetCore.HealthChecks.UI**: Health check UI and system checks
- **Moq**: Mocking framework for tests
- **FluentAssertions**: Fluent assertion syntax for tests

### Framework

- .NET 10 (Latest LTS)
- ASP.NET Core 10
- C# 13

## Migration from Windows Forms

All logic from the legacy WinClienteSEIDA application has been successfully migrated to this Web API:

| Feature | Legacy Location | New Location | Status |
|---------|-----------------|--------------|--------|
| File Submission | `ThreadProxy.OnStartSend()` | `SubmissionService` | ✅ Migrated |
| Status Query | `ThreadProxy.OnStartQuery()` | `QueryService` | ✅ Migrated |
| Error Parsing | `Core.EvaluateMessageError()` | `SeidaClient` | ✅ Migrated |
| AES Encryption | `Core.Encrypt/Decrypt()` | `AuthenticationService` | ✅ Migrated |
| File Naming | `Core.GenerateName*()` | `FileService` | ✅ Migrated |
| SOAP Headers | `CustomHeadersAssertion` | Refit configuration | ✅ Migrated |
| Proxy Support | Manual WebProxy | Configuration + Refit | ✅ Migrated |
| Network Info | `Core.GetIP/MAC()` | `NetworkService` | ✅ Migrated |
| Logging | `Trace.WriteLine()` | `ILogger` | ✅ Upgraded |
| Configuration | `App.config` | `appsettings.json` | ✅ Modernized |

## Known Limitations & Future Enhancements

### Current Limitations

- No database persistence (in-memory operation)
- No audit trail/transaction history
- No batch operation scheduling
- Limited rate limiting

### Future Enhancements

1. **Persistence**: Add Entity Framework Core with SQL Server/PostgreSQL
2. **Message Queue**: Integrate RabbitMQ/Azure Service Bus for async processing
3. **Batch Operations**: Schedule and monitor batch file submissions
4. **Webhook Support**: Notify clients of submission status changes
5. **Audit Trail**: Complete operation history and compliance logging
6. **Rate Limiting**: Prevent abuse and enforce quotas
7. **Advanced Retry**: Circuit breaker and exponential backoff for resilience
8. **Multi-tenancy**: Support multiple SUNAT organizations
9. **Caching**: Response caching for frequently queried tickets

## Contributing

When contributing to this project:

1. Follow Clean Code principles
2. Add unit tests for new features (target 80%+ coverage)
3. Update `appsettings.json` documentation
4. Add XML comments to public APIs
5. Use dependency injection for all services

## License

[Specify your license here]

## Support

For issues or questions:

1. Check the [Swagger documentation](http://localhost:5000/swagger) when running locally
2. Review the [test cases](Hanseatica.Api.Seida.Tests/) for usage examples
3. Check the health endpoints for system diagnostics

## Related Documents

- [CLAUDE.md](../WinClienteSEIDA/CLAUDE.md) - Original Windows Forms project documentation
- [ProxyClient.cs](../WinClienteSEIDA/WinClienteSEIDA/Logic/ProxyClient.cs) - Original SEIDA client proxy
