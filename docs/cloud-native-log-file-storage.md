# Cloud-Native Log Management and File Storage (Azure)

## Context

The original WinClienteSEIDA desktop application managed:
- **Logs**: A single `BitacoraEventos.txt` file with timestamped events (submissions, queries, errors, network info — IP/MAC)
- **Files**: Three working directories — `destino_acuse/` (receipt XMLs), `destino_respuesta/` (query response files), `origen_documento/` (input ZIPs)

The current .NET 10 API replaced the desktop app but still uses local filesystem patterns:
- `FileService` writes/reads files via `System.IO.File`
- Logging uses only `AddConsole()` and `AddDebug()` providers

For Azure/AKS deployment these are insufficient: a pod has no persistent local disk, logs are not queryable, and files are lost on restart. This document describes the migration to cloud-native equivalents.

---

## Part 1 — Structured Logging with Serilog + Application Insights

### Packages (`Hanseatica.Api.Seida.Api.csproj`)

| Package | Purpose |
|---|---|
| `Serilog.AspNetCore` | `UseSerilog()` host extension + HTTP request logging |
| `Serilog.Formatting.Compact` | CLEF JSON formatter for K8s console output |
| `Serilog.Sinks.File` | Rolling file sink for local development |
| `Serilog.Sinks.ApplicationInsights` | Push logs to Azure Application Insights |
| `Serilog.Enrichers.Environment` | Adds `MachineName` to every log event |
| `Serilog.Enrichers.Thread` | Adds `ThreadId` (mirrors WinClienteSEIDA's "Hilo" concept) |

### Program.cs

Replaced `AddLogging(config => ...)` with:

```csharp
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId());
```

### appsettings.json — Serilog section

```json
"Serilog": {
  "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.ApplicationInsights"],
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft.AspNetCore": "Warning",
      "System.Net.Http": "Warning",
      "Hanseatica": "Debug"
    }
  },
  "WriteTo": [
    {
      "Name": "Console",
      "Args": {
        "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
      }
    },
    {
      "Name": "ApplicationInsights",
      "Args": {
        "connectionString": "",
        "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"
      }
    }
  ],
  "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
}
```

### appsettings.Development.json — adds File sink

```json
"Serilog": {
  "WriteTo": [
    { "Name": "Console", "Args": { "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact" } },
    { "Name": "File", "Args": { "path": "logs/seida-.log", "rollingInterval": "Day", "retainedFileCountLimit": 7 } }
  ]
}
```

### Structured event mapping

| BitacoraEventos.txt event | Code location | Serilog result |
|---|---|---|
| `IP: {ip} / MAC: {mac}` | `SeidaClientOptions` + enrichers | `MachineName`, `ThreadId` properties on every event |
| `Enviando Hilo(n) | Nro.Transaccion:{id}` | `SeidaClient.SubmitFileAsync` | `{TransactionNumber}` structured property |
| `Respuesta Hilo(n) | Ticket:{ticket}` | `SeidaClient.SubmitFileAsync` | `{TicketNumber}`, `{ErrorCount}` properties |
| `Consulta Hilo(n) | Año:{year} | Ticket:{ticket}` | `SeidaClient.QueryStatusAsync` | `{Year}`, `{TicketNumber}` properties |
| `Respuesta Hilo(n) | Ticket:{ticket} | Archivo:{path}` | `SeidaClient.QueryStatusAsync` | `{StatusType}`, `{Description}` properties |
| `Error OnStartSend:{message}` | `SeidaClient` catch blocks | `_logger.LogError` already present |

---

## Part 2 — Azure Blob Storage for File Persistence

### Azure Blob Container Mapping

| WinClienteSEIDA Directory | Azure Blob Container |
|---|---|
| `destino_acuse/` | `seida-acuse` |
| `destino_respuesta/` | `seida-respuesta` |

File naming conventions are unchanged (`seida_acuse_{year}_{ticket}.xml`, `seida_respuesta_{year}_{ts}_{ticket}.xml`).

### Package (`Hanseatica.Api.Seida.Infrastructure.csproj`)

- `Azure.Storage.Blobs`

### New: `IFileStorageProvider` (Application layer)

```csharp
public interface IFileStorageProvider
{
    Task<string> SaveAsync(string containerName, string blobName, byte[] content, CancellationToken cancellationToken = default);
    Task<byte[]> LoadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
```

### New: `FileStorageOptions`

```csharp
public class FileStorageOptions
{
    public string Provider { get; set; } = "Local";      // "Local" | "AzureBlob"
    public string LocalBasePath { get; set; } = "data/seida";
    public string AzureBlobConnectionString { get; set; } = string.Empty;
    public string AcuseContainer { get; set; } = "seida-acuse";
    public string RespuestaContainer { get; set; } = "seida-respuesta";
}
```

### New implementations

- **`LocalFileStorageProvider`**: wraps `File.WriteAllBytesAsync` / `ReadAllBytesAsync` under `{LocalBasePath}/{containerName}/{blobName}`
- **`AzureBlobStorageProvider`**: uses `BlobServiceClient` — `UploadAsync` / `DownloadContentAsync`, creates containers if absent

### appsettings.json additions

```json
"FileStorage": {
  "Provider": "AzureBlob",
  "AzureBlobConnectionString": "",
  "AcuseContainer": "seida-acuse",
  "RespuestaContainer": "seida-respuesta"
}
```

### appsettings.Development.json override

```json
"FileStorage": {
  "Provider": "Local",
  "LocalBasePath": "data/seida"
}
```

### Modified files

| File | Change |
|---|---|
| `Application/Interfaces/IFileStorageProvider.cs` | New |
| `Infrastructure/Configuration/FileStorageOptions.cs` | New |
| `Infrastructure/Services/LocalFileStorageProvider.cs` | New |
| `Infrastructure/Services/AzureBlobStorageProvider.cs` | New |
| `Infrastructure/Services/FileService.cs` | Inject `IFileStorageProvider`; delegate Save/Load |
| `Infrastructure/DependencyInjection.cs` | Register `FileStorageOptions` + provider by config |
| `Infrastructure/Hanseatica.Api.Seida.Infrastructure.csproj` | Add `Azure.Storage.Blobs` |

---

## Verification

1. `dotnet build` — 0 errors, 0 warnings
2. `dotnet test` — all 188 existing tests pass
3. Local run with `FileStorage:Provider = "Local"` — files written to `data/seida/seida-acuse/` and `data/seida/seida-respuesta/`
4. Console output is CLEF JSON with `MachineName` and `ThreadId` properties
5. `FileStorage:Provider = "AzureBlob"` with valid connection string — blobs visible in Azure Storage Explorer
6. `Serilog:WriteTo:ApplicationInsights:connectionString` configured — events appear in Application Insights Live Metrics
