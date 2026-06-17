# Getting Started with Hanseatica.Api.Seida

This guide will help you set up and run the SEIDA Web API for the first time.

## Prerequisites

- **.NET 10 SDK** or later
  - Download from: https://dotnet.microsoft.com/download/dotnet
  - Verify installation: `dotnet --version`

- **Git** (optional, for cloning)
- **A Code Editor** (VS Code, Visual Studio, JetBrains Rider, etc.)

## Quick Start (5 minutes)

### 1. Build the Solution

```bash
cd C:\Hanseatica.Api.Seida
dotnet build
```

Expected output:
```
Build succeeded. 0 Warning(s)
```

### 2. Run Tests (Optional but Recommended)

```bash
dotnet test
```

Expected output:
```
Test Run Successful.
Total tests: 67
     Passed: 67
     Failed: 0
```

### 3. Start the API

```bash
cd Hanseatica.Api.Seida.Api
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to stop the application, sdrv, SdrvVersion=10.0, SdrvSP=1
```

### 4. Access the API

- **Swagger UI** (API Documentation & Testing): http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **API Base URL**: http://localhost:5000/api

## Detailed Setup

### Step 1: Extract/Clone the Solution

If you have a ZIP file:
```bash
# Extract the archive
# Navigate to the extracted folder
cd Hanseatica.Api.Seida
```

### Step 2: Review Project Structure

```
Hanseatica.Api.Seida/
├── Hanseatica.Api.Seida.Api/              # REST API endpoints
│   ├── Controllers/                       # SubmissionsController, QueriesController
│   ├── Program.cs                         # API startup configuration
│   ├── appsettings.json                   # Configuration
│   └── appsettings.Development.json       # Development overrides
│
├── Hanseatica.Api.Seida.Application/      # Business logic layer
│   ├── Services/                          # SubmissionService, QueryService
│   ├── DTOs/                              # Request/Response data classes
│   ├── Interfaces/                        # Service contracts
│   └── Exceptions/                        # Custom exceptions
│
├── Hanseatica.Api.Seida.Domain/           # Core domain models
│   ├── Entities/                          # Submission, Receipt, Query, etc.
│   ├── ValueObjects/                      # SecurityCredentials, FileInfo
│   └── Enums/                             # QueryStatusType
│
├── Hanseatica.Api.Seida.Infrastructure/   # External integrations
│   ├── ExternalServices/                  # ISeidaHttpClient (Refit)
│   ├── Services/                          # Implementations of interfaces
│   └── ...
│
├── Hanseatica.Api.Seida.Tests/            # Unit tests
│   ├── Unit/                              # Test files
│   ├── Fixtures/                          # Test data builders
│   └── ...
│
├── README.md                              # Full project documentation
├── GETTING_STARTED.md                     # This file
└── Hanseatica.Api.Seida.sln              # Solution file
```

### Step 3: Configure External Services

The API needs to connect to SUNAT's SEIDA service. Configuration is in `appsettings.json`:

#### Development Configuration (Sandbox Testing)

The file `appsettings.Development.json` is already configured for the test environment:

```json
{
  "Seida": {
    "BaseUrl": "http://pruebaaduana.sunat.gob.pe/ol-ad-itseida-ws",
    "Username": "PILOTONS",
    "Password": "change_me",
    "TimeoutSeconds": 30
  }
}
```

To use development settings:
```bash
dotnet run --environment Development
```

#### Production Configuration

To use the production SEIDA service, update `appsettings.json`:

```json
{
  "Seida": {
    "BaseUrl": "https://www.sunat.gob.pe/ol-ad-itseida-ws",
    "Username": "your_username",
    "Password": "your_password",
    "TimeoutSeconds": 30
  }
}
```

⚠️ **Security Note**: Never commit credentials to git. Use environment variables instead:

```bash
# Linux/macOS
export Seida__Username=your_username
export Seida__Password=your_password

# Windows PowerShell
$env:Seida__Username="your_username"
$env:Seida__Password="your_password"

# Windows CMD
set Seida__Username=your_username
set Seida__Password=your_password
```

### Step 4: Build and Run

```bash
# Build the solution
dotnet build

# Run the tests (recommended)
dotnet test

# Run the API
cd Hanseatica.Api.Seida.Api
dotnet run

# Or run in development mode with verbose logging
dotnet run --environment Development
```

### Step 5: Test the API

Open your browser to: http://localhost:5000/swagger

You'll see the interactive API documentation. Here are some examples:

#### Example 1: Submit a File

1. Click on **POST /api/submissions/generate-transaction-id**
2. Click **Try it out**
3. Click **Execute** to generate a transaction ID
4. Copy the `transactionId` from the response
5. Create a test ZIP file (or use an existing one)
6. Click on **POST /api/submissions/submit-with-transaction**
7. Enter the `transactionId`
8. Click **Choose File** and select your ZIP file
9. Click **Execute**
10. You should receive a receipt with a ticket number

#### Example 2: Query a Submission

1. Click on **GET /api/queries/query-status**
2. Enter `year`: "2024"
3. Enter `ticketNumber`: (use a ticket from a previous submission)
4. Click **Execute**
5. You'll receive the status of that submission

## Health Checks

Monitor the API health:

```bash
# Check overall health
curl http://localhost:5000/health

# Check readiness (external dependencies)
curl http://localhost:5000/health/ready
```

Example response:
```json
{
  "status": "Healthy",
  "checks": {
    "disk": {
      "status": "Healthy"
    },
    "seida-service": {
      "status": "Healthy"
    }
  }
}
```

## Running Tests

### All Tests

```bash
dotnet test
```

### Tests with Output

```bash
dotnet test --verbosity normal
```

### Specific Test Class

```bash
dotnet test --filter ClassName=SubmissionServiceTests
```

### Tests with Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Common Tasks

### Task: Add a New API Endpoint

1. Create a new action in a controller (e.g., `SubmissionsController`)
2. Implement the business logic in a service (e.g., `ISubmissionService`)
3. Add unit tests for the new logic
4. Update the `appsettings.json` if new configuration is needed

### Task: Change Configuration

All configuration is in `appsettings.json` and `appsettings.Development.json`. No changes to code are needed.

### Task: Enable/Disable Proxy

Edit `appsettings.json`:

```json
{
  "Seida": {
    "ProxyEnabled": true,
    "ProxyAddress": "proxy.company.com",
    "ProxyPort": 3128,
    "ProxyUsername": "username",
    "ProxyPassword": "password"
  }
}
```

(Note: Proxy configuration is currently defined in settings but full implementation may need code changes)

### Task: Change Service Timeout

Edit `appsettings.json`:

```json
{
  "Seida": {
    "TimeoutSeconds": 60  // Default is 30 seconds
  }
}
```

## Troubleshooting

### "Port 5000 already in use"

The API tries to use port 5000 by default. If it's already in use, you can specify a different port:

```bash
dotnet run -- --urls "http://localhost:5001"
```

### "Cannot connect to SEIDA service"

1. Check your network connectivity
2. Verify the `BaseUrl` in `appsettings.json` is correct
3. Check credentials are correct in `appsettings.json` or environment variables
4. Verify the SEIDA service is accessible (try in a browser/curl)

### "Tests are failing"

1. Make sure you've built the solution: `dotnet build`
2. Check that all NuGet packages are restored: `dotnet restore`
3. Run tests with verbose output: `dotnet test --verbosity normal`

### "Swagger page won't load"

1. Make sure the API is running: `dotnet run`
2. Check you're accessing the correct URL: http://localhost:5000/swagger
3. Try building the solution: `dotnet build`

## Next Steps

1. **Read the full documentation**: See [README.md](README.md)
2. **Understand the architecture**: Review the code structure and domain models
3. **Integrate with your system**: Add this API to your application
4. **Deploy to production**: Follow your deployment procedures
5. **Monitor health**: Set up monitoring for `/health` endpoint
6. **Add persistence**: Integrate a database (Entity Framework Core)

## Getting Help

### Documentation

- **API Docs**: http://localhost:5000/swagger (when running)
- **Full README**: [README.md](README.md)
- **Test Examples**: [Hanseatica.Api.Seida.Tests/](Hanseatica.Api.Seida.Tests/)

### Source Code

The code is organized by layer:

- **API Layer**: `Controllers/` - REST endpoint implementations
- **Application Layer**: `Application/Services/` - Business logic
- **Domain Layer**: `Domain/Entities/` - Core business models
- **Infrastructure Layer**: `Infrastructure/Services/` - External service integrations

Each file has XML comments explaining its purpose.

### Common Files to Review

1. `Hanseatica.Api.Seida.Api/Program.cs` - API startup and configuration
2. `Hanseatica.Api.Seida.Api/Controllers/SubmissionsController.cs` - File submission endpoints
3. `Hanseatica.Api.Seida.Application/Services/SubmissionService.cs` - Business logic
4. `Hanseatica.Api.Seida.Infrastructure/Services/SeidaClient.cs` - External service integration

## Additional Resources

- [.NET 10 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [SUNAT SEIDA Documentation](https://www.sunat.gob.pe/) (Spanish)

---

**You're all set!** 🎉

Start the API with `dotnet run`, open http://localhost:5000/swagger, and begin testing the SEIDA integration.
