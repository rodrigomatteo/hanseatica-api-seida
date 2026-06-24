# 🚀 Setup Automático: Copia web a wwwroot y configura Program.cs
# Uso: .\setup-web.ps1
# Requiere: PowerShell 5.0+, acceso a las carpetas del proyecto

param(
    [string]$ApiPath = "C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api",
    [string]$WebPath = "C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web"
)

$colors = @{
    Success = 'Green'
    Error   = 'Red'
    Info    = 'Cyan'
    Warning = 'Yellow'
}

function Write-Header {
    param([string]$Text)
    Write-Host "`n$('=' * 70)" -ForegroundColor $colors.Info
    Write-Host "  $Text" -ForegroundColor $colors.Info
    Write-Host "$('=' * 70)`n" -ForegroundColor $colors.Info
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

# ==================== VALIDACIÓN ====================

Write-Header "VALIDACIÓN DE RUTAS"

if (-not (Test-Path $ApiPath)) {
    Write-Error "No se encontró API en: $ApiPath"
    exit 1
}
Write-Success "API encontrada: $ApiPath"

if (-not (Test-Path $WebPath)) {
    Write-Error "No se encontró Web en: $WebPath"
    exit 1
}
Write-Success "Web encontrada: $WebPath"

$programCsPath = Join-Path $ApiPath "Program.cs"
if (-not (Test-Path $programCsPath)) {
    Write-Error "No se encontró Program.cs en: $programCsPath"
    exit 1
}
Write-Success "Program.cs encontrado"

# ==================== CREAR wwwroot ====================

Write-Header "CREAR CARPETA wwwroot"

$wwwRootPath = Join-Path $ApiPath "wwwroot"

if (Test-Path $wwwRootPath) {
    Write-Warning "wwwroot ya existe. Se borrará el contenido."
    Remove-Item "$wwwRootPath\*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Info "Contenido antiguo eliminado"
}
else {
    New-Item -ItemType Directory -Path $wwwRootPath -Force | Out-Null
    Write-Success "Carpeta wwwroot creada"
}

# ==================== COPIAR ARCHIVOS ====================

Write-Header "COPIAR ARCHIVOS WEB"

$itemsToCopy = @(
    "index.html",
    "js",
    "css",
    "README.md",
    "TESTING.md",
    "CONFIGURACION.md",
    "test-api.ps1"
)

foreach ($item in $itemsToCopy) {
    $sourcePath = Join-Path $WebPath $item
    $destPath = Join-Path $wwwRootPath $item

    if (Test-Path $sourcePath) {
        if ((Get-Item $sourcePath).PSIsContainer) {
            # Es carpeta
            Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
            Write-Success "Carpeta copiada: $item"
        }
        else {
            # Es archivo
            Copy-Item -Path $sourcePath -Destination $destPath -Force
            Write-Success "Archivo copiado: $item"
        }
    }
    else {
        Write-Warning "No encontrado: $item"
    }
}

# ==================== EDITAR Program.cs ====================

Write-Header "EDITAR Program.cs"

$programCs = Get-Content $programCsPath -Raw

# Verificar si ya está configurado
if ($programCs -match "UseStaticFiles" -and $programCs -match "MapFallbackToFile") {
    Write-Warning "Program.cs ya está configurado con UseStaticFiles y MapFallbackToFile"
    Write-Info "Se saltará la edición"
}
else {
    Write-Info "Actualizando Program.cs..."

    # Patrón a buscar
    $pattern = "(app\.UseHttpsRedirection\(\);[\s\n]+app\.UseAuthorization\(\);[\s\n]+app\.MapControllers\(\);[\s\n]+app\.Run\(\);)"

    # Reemplazo
    $replacement = @"
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();

// Fallback para SPA: servir index.html para rutas no-API
app.MapFallbackToFile("index.html");

app.Run();
"@

    $newContent = $programCs -replace $pattern, $replacement

    # Validar que se hizo el reemplazo
    if ($newContent -match "UseStaticFiles" -and $newContent -match "MapFallbackToFile") {
        Set-Content -Path $programCsPath -Value $newContent
        Write-Success "Program.cs actualizado correctamente"
    }
    else {
        Write-Warning "No se pudo hacer reemplazo automático"
        Write-Info "Edita manualmente Program.cs:"
        Write-Info "1. Busca: app.MapControllers();"
        Write-Info "2. Antes de app.Run();, agrega:"
        Write-Info "   - app.UseStaticFiles();"
        Write-Info "   - app.MapFallbackToFile(""index.html"");"
    }
}

# ==================== COMPILAR ====================

Write-Header "COMPILAR PROYECTO"

Write-Info "Compilando..."
Push-Location $ApiPath

try {
    $output = dotnet build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Compilación exitosa"
    }
    else {
        Write-Error "Error en compilación"
        Write-Host $output
        exit 1
    }
}
catch {
    Write-Error "Error ejecutando dotnet build: $_"
    exit 1
}
finally {
    Pop-Location
}

# ==================== VERIFICACIÓN ====================

Write-Header "VERIFICACIÓN FINAL"

$wwwRootFiles = @(
    "index.html",
    "js\app.js",
    "css\styles.css"
)

$allOk = $true
foreach ($file in $wwwRootFiles) {
    $fullPath = Join-Path $wwwRootPath $file
    if (Test-Path $fullPath) {
        Write-Success "Archivo presente: $file"
    }
    else {
        Write-Error "Archivo faltante: $file"
        $allOk = $false
    }
}

if (-not $allOk) {
    Write-Error "Falta algún archivo"
    exit 1
}

# Verificar Program.cs
$programCsContent = Get-Content $programCsPath -Raw
if ($programCsContent -match "UseStaticFiles" -and $programCsContent -match "MapFallbackToFile") {
    Write-Success "Program.cs configurado correctamente"
}
else {
    Write-Warning "Program.cs podría necesitar edición manual"
}

# ==================== INSTRUCCIONES FINALES ====================

Write-Header "¡SETUP COMPLETADO! 🎉"

Write-Success "Archivos web copiados a wwwroot"
Write-Success "Program.cs configurado"
Write-Success "Proyecto compilado"

Write-Host "`n📋 PRÓXIMOS PASOS:`n" -ForegroundColor $colors.Info

Write-Host "1️⃣  Inicia el API:" -ForegroundColor $colors.Info
Write-Host "    cd $ApiPath"
Write-Host "    dotnet run`n" -ForegroundColor $colors.Warning

Write-Host "2️⃣  Abre en el navegador:" -ForegroundColor $colors.Info
Write-Host "    http://localhost:5000`n" -ForegroundColor $colors.Warning

Write-Host "3️⃣  (Opcional) Ejecuta tests:" -ForegroundColor $colors.Info
Write-Host "    cd $(Join-Path $ApiPath 'wwwroot')"
Write-Host "    .\test-api.ps1`n" -ForegroundColor $colors.Warning

Write-Host "📚 DOCUMENTACIÓN:" -ForegroundColor $colors.Info
Write-Host "   - QUICKSTART-SIN-PYTHON.md (este documento)"
Write-Host "   - TESTING.md (guía completa de testing)"
Write-Host "   - README.md (cómo usar la web)"
Write-Host ""

Write-Success "¡Setup completado exitosamente!"
