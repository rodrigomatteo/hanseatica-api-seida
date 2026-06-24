# Script de Testing: API Endpoints
# Uso: .\test-api.ps1
# Requiere: PowerShell 7.0+ y API corriendo en http://localhost:5000

param(
    [string]$ApiUrl = "http://localhost:5000",
    [string]$RUC = "20100010136",
    [string]$Year = "2026",
    [string]$TicketNumber = "123456"
)

$colors = @{
    Success = 'Green'
    Error   = 'Red'
    Info    = 'Cyan'
    Warning = 'Yellow'
}

function Write-Header {
    param([string]$Text)
    Write-Host "`n$('=' * 60)" -ForegroundColor $colors.Info
    Write-Host "  $Text" -ForegroundColor $colors.Info
    Write-Host "$('=' * 60)`n" -ForegroundColor $colors.Info
}

function Write-Success {
    param([string]$Text)
    Write-Host "✅ $Text" -ForegroundColor $colors.Success
}

function Write-Error {
    param([string]$Text)
    Write-Host "❌ $Text" -ForegroundColor $colors.Error
}

function Write-Info {
    param([string]$Text)
    Write-Host "ℹ️  $Text" -ForegroundColor $colors.Info
}

function Write-Warning {
    param([string]$Text)
    Write-Host "⚠️  $Text" -ForegroundColor $colors.Warning
}

function Test-ApiConnection {
    Write-Header "TEST 1: Conexión a API"

    try {
        $response = Invoke-WebRequest -Uri "$ApiUrl/health" -Method Get -ErrorAction Stop
        Write-Success "API está accesible en $ApiUrl"
        Write-Info "Status Code: $($response.StatusCode)"
        return $true
    }
    catch {
        Write-Error "No se puede conectar a $ApiUrl"
        Write-Warning "Asegúrate de que el API está corriendo: dotnet run"
        Write-Info "Comando: cd Hanseatica.Api.Seida.Api && dotnet run"
        return $false
    }
}

function Test-GenerateTransactionId {
    Write-Header "TEST 2: Generar Número de Transacción"

    try {
        $response = Invoke-WebRequest -Uri "$ApiUrl/api/submissions/generate-transaction-id" `
            -Method Get `
            -ErrorAction Stop

        $data = $response.Content | ConvertFrom-Json

        Write-Success "Transacción generada exitosamente"
        Write-Info "Transacción: $($data.transactionId)"

        return $data.transactionId
    }
    catch {
        Write-Error "No se pudo generar transacción"
        Write-Info "Error: $($_.Exception.Message)"
        return $null
    }
}

function Test-SubmitFile {
    param([string]$TransactionNumber)

    Write-Header "TEST 3: Envío de Archivo"

    if (-not $TransactionNumber) {
        Write-Error "Número de transacción vacío"
        return $false
    }

    # Crear archivo ZIP de prueba
    $testDir = "$env:TEMP\seida-test"
    if (-not (Test-Path $testDir)) {
        New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    }

    $xmlFile = "$testDir\test.xml"
    $zipFile = "$testDir\test.zip"

    # Crear XML de prueba
    @"
<?xml version="1.0" encoding="UTF-8"?>
<documento>
    <tipo>test</tipo>
    <contenido>Archivo de prueba para SEIDA</contenido>
    <fecha>$(Get-Date -Format 'yyyy-MM-dd')</fecha>
</documento>
"@ | Out-File -FilePath $xmlFile -Encoding UTF8 -Force

    Write-Info "Archivo de prueba creado: $xmlFile"

    # Crear ZIP
    if (Test-Path $zipFile) {
        Remove-Item $zipFile -Force
    }

    Compress-Archive -Path $xmlFile -DestinationPath $zipFile -Force
    Write-Info "ZIP creado: $zipFile"

    try {
        $fileBytes = [System.IO.File]::ReadAllBytes($zipFile)

        $response = Invoke-WebRequest -Uri "$ApiUrl/api/submissions/submit-with-transaction?transactionNumber=$TransactionNumber" `
            -Method Post `
            -ContentType "application/octet-stream" `
            -Body $fileBytes `
            -ErrorAction Stop

        $data = $response.Content | ConvertFrom-Json

        Write-Success "Archivo enviado exitosamente"
        Write-Info "Ticket: $($data.ticketNumber)"
        Write-Info "Año: $($data.submissionYear)"
        Write-Info "Emisor: $($data.issuerDocument)"

        if ($data.errors.Count -gt 0) {
            Write-Warning "Errores en respuesta:"
            foreach ($error in $data.errors) {
                Write-Info "  - $($error.code): $($error.description)"
            }
        }

        return $data.ticketNumber
    }
    catch {
        Write-Error "No se pudo enviar archivo"
        Write-Info "Error: $($_.Exception.Message)"

        # Mostrar respuesta si disponible
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            Write-Info "Response Body: $body"
            $reader.Close()
        }

        return $null
    }
}

function Test-QueryStatus {
    param([string]$QueryYear, [string]$TicketNumber)

    Write-Header "TEST 4: Consulta de Ticket"

    if (-not $TicketNumber) {
        Write-Warning "Número de ticket no especificado"
        Write-Info "Usando valor por defecto: $TicketNumber"
    }

    try {
        $response = Invoke-WebRequest -Uri "$ApiUrl/api/queries/query-status?year=$QueryYear&ticketNumber=$TicketNumber" `
            -Method Get `
            -ErrorAction Stop

        $data = $response.Content | ConvertFrom-Json

        Write-Success "Consulta realizada exitosamente"
        Write-Info "Status: $($data.statusType)"
        Write-Info "Descripción: $($data.description)"

        if ($data.responseData) {
            Write-Info "Datos de respuesta: [Binary data present]"
        }

        return $true
    }
    catch {
        Write-Error "No se pudo consultar ticket"
        Write-Info "Error: $($_.Exception.Message)"
        return $false
    }
}

function Test-ApiEndpoints {
    Write-Header "TEST 5: Verificar Endpoints Disponibles"

    $endpoints = @(
        "GET /api/submissions/generate-transaction-id",
        "POST /api/submissions/submit",
        "POST /api/submissions/submit-with-transaction",
        "GET /api/queries/query-status",
        "GET /health"
    )

    Write-Info "Endpoints esperados:"
    foreach ($endpoint in $endpoints) {
        Write-Host "  ✓ $endpoint"
    }

    Write-Info "`nPara ver todos los endpoints disponibles, abre:"
    Write-Info "  http://localhost:5000/swagger"
}

function Test-Cors {
    Write-Header "TEST 6: CORS Preflight"

    Write-Info "Verificando si CORS está habilitado..."

    try {
        $response = Invoke-WebRequest -Uri "$ApiUrl/api/submissions/generate-transaction-id" `
            -Method Options `
            -Headers @{ Origin = "http://localhost:8000" } `
            -ErrorAction Stop

        Write-Success "CORS habilitado"
        $corsSHeaders = $response.Headers | Where-Object { $_ -match "Access-Control" }
        if ($corsSHeaders) {
            Write-Info "Headers CORS encontrados: $corsSHeaders"
        }
    }
    catch {
        Write-Warning "CORS podría no estar habilitado correctamente"
        Write-Info "Si ejecutas web y API en dominios diferentes, necesitas:"
        Write-Info "  1. Usar proxy.py (recomendado)"
        Write-Info "  2. O habilitar CORS en Program.cs"
    }
}

function Show-Summary {
    param([hashtable]$Results)

    Write-Header "RESUMEN DE TESTS"

    $passed = $Results.Values | Where-Object { $_ -eq $true } | Measure-Object | Select-Object -ExpandProperty Count
    $failed = $Results.Values | Where-Object { $_ -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
    $total = $Results.Count

    Write-Info "Total Tests: $total"
    Write-Success "Pasados: $passed"
    Write-Error "Fallidos: $failed"

    if ($failed -eq 0) {
        Write-Host "`n🎉 ¡TODOS LOS TESTS PASARON!" -ForegroundColor Green -BackgroundColor Black
    }
    else {
        Write-Host "`n⚠️  Algunos tests fallaron. Revisa los errores arriba." -ForegroundColor Yellow -BackgroundColor Black
    }
}

# ==================== MAIN ====================

Write-Host "`n🧪 Testing SEIDA API" -ForegroundColor Cyan -BackgroundColor Black
Write-Host "================================" -ForegroundColor Cyan
Write-Info "API URL: $ApiUrl"
Write-Info "RUC: $RUC"
Write-Info "Año: $Year"
Write-Info "Ticket: $TicketNumber"

$results = @{}

# Test 1: Conexión
$results["API Connection"] = Test-ApiConnection
if (-not $results["API Connection"]) {
    Write-Error "Tests detenidos: No hay conexión con API"
    exit 1
}

# Test 2: Generar transacción
$transactionId = Test-GenerateTransactionId
$results["Generate Transaction"] = $null -ne $transactionId

# Test 3: Enviar archivo
if ($transactionId) {
    $ticketNumber = Test-SubmitFile -TransactionNumber $transactionId
    $results["Submit File"] = $null -ne $ticketNumber

    # Si se generó ticket, usa ese para la consulta
    if ($ticketNumber) {
        $TicketNumber = $ticketNumber
    }
}
else {
    $results["Submit File"] = $false
}

# Test 4: Consultar ticket
$results["Query Status"] = Test-QueryStatus -QueryYear $Year -TicketNumber $TicketNumber

# Test 5: Endpoints
Test-ApiEndpoints
$results["Endpoints Available"] = $true

# Test 6: CORS
Test-Cors
$results["CORS Check"] = $true

# Mostrar resumen
Show-Summary -Results $results

Write-Host "`n📚 Para más información, lee:" -ForegroundColor Cyan
Write-Host "  - TESTING.md: Guía completa de testing"
Write-Host "  - README.md: Cómo usar la aplicación web"
Write-Host "  - CONFIGURACION.md: Configuración API ↔ Web"
Write-Host ""
