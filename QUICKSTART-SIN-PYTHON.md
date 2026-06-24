# 🚀 Quick Start: SIN Python (Solo .NET)

Elige una opción:

---

## ✅ OPCIÓN 1: Más Fácil (RECOMENDADO)
### Servir web desde wwwroot del API

### Paso 1: Copiar archivos web a wwwroot

**PowerShell:**
```powershell
# Crear wwwroot si no existe
New-Item -ItemType Directory -Path "C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api\wwwroot" -Force

# Copiar archivos web
Copy-Item -Path "C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web\*" `
          -Destination "C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api\wwwroot" `
          -Recurse -Force

# Verificar
Get-ChildItem "C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api\wwwroot"
```

**Resultado esperado:**
```
Mode                 Name
----                 ----
d-----           css
d-----           js
-a----       index.html
-a----       README.md
-a----       TESTING.md
-a----       CONFIGURACION.md
-a----       test-api.ps1
```

---

### Paso 2: Actualizar Program.cs

**Archivo:** `C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api\Program.cs`

Encuentra esta línea (alrededor de línea 42):
```csharp
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

Reemplázalo por:
```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();

// Fallback para SPA: si la ruta no es /api/*, servir index.html
app.MapFallbackToFile("index.html");

app.Run();
```

---

### Paso 3: Compilar

**PowerShell:**
```powershell
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
dotnet build
```

**Salida esperada:**
```
Build succeeded.
    0 Error(s)
```

---

### Paso 4: Ejecutar

**PowerShell:**
```powershell
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
dotnet run
```

**Salida esperada:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

---

### Paso 5: Abrir en navegador

```
http://localhost:5000
```

✅ **¡Listo!** Todo en un solo proceso.

---

## ✅ OPCIÓN 2: Fácil (IIS Express)

### Paso 1: Crear carpeta de la web

```powershell
# Visual Studio automáticamente detectará la carpeta
# Si usas VS, abre la solución y los archivos estarán disponibles
```

### Paso 2: En Visual Studio

1. Abre `Hanseatica.Api.Seida.sln` en Visual Studio
2. Click derecho en proyecto `Hanseatica.Api.Seida.Api`
3. **Set as Startup Project**
4. Presiona **F5** (o Debug → Start Debugging)
5. Se abre automáticamente `https://localhost:5001`

⚠️ **Si la web no aparece:** Ejecuta el Paso 1 de OPCIÓN 1 primero.

---

## ✅ OPCIÓN 3: Más Control (HTTP Server en .NET)

### Crear un simple HTTP server en C#

**Archivo:** `C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web\WebServer.cs`

```csharp
using System;
using System.Net;
using System.IO;
using System.Linq;

class SimpleWebServer
{
    static void Main(string[] args)
    {
        int port = 8000;
        string webRoot = AppContext.BaseDirectory;
        
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        
        Console.WriteLine($"🌐 Web Server: http://localhost:{port}");
        Console.WriteLine($"📁 Sirviendo desde: {webRoot}");
        Console.WriteLine("🛑 Presiona Ctrl+C para detener\n");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string path = request.Url.LocalPath;
                if (path == "/") path = "/index.html";

                string filePath = Path.Combine(webRoot, path.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    response.ContentLength64 = fileBytes.Length;
                    response.ContentType = GetMimeType(filePath);
                    response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                    response.StatusCode = 200;
                }
                else
                {
                    // Fallback a index.html para SPA routing
                    string indexPath = Path.Combine(webRoot, "index.html");
                    if (File.Exists(indexPath))
                    {
                        byte[] fileBytes = File.ReadAllBytes(indexPath);
                        response.ContentLength64 = fileBytes.Length;
                        response.ContentType = "text/html";
                        response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                        response.StatusCode = 200;
                    }
                    else
                    {
                        response.StatusCode = 404;
                    }
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {request.HttpMethod} {path} → {response.StatusCode}");
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                response.OutputStream.Close();
            }
        }
    }

    static string GetMimeType(string filePath)
    {
        return Path.GetExtension(filePath).ToLower() switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            ".svg" => "image/svg+xml",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
```

Luego ejecutar:
```bash
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web
dotnet run WebServer.cs
```

Acceder a: `http://localhost:8000`

---

## 🧪 Testing después de setup

### Terminal 1: Backend (API)

```powershell
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
dotnet run
```

Verás:
```
Now listening on: http://localhost:5000
```

---

### Terminal 2: Tests

**PowerShell:**
```powershell
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web
.\test-api.ps1
```

---

### Navegador

**OPCIÓN 1 (Recomendada):**
```
http://localhost:5000
```

---

## 📋 Paso a Paso OPCIÓN 1 (Recomendada)

### 1. Copiar archivos web

```powershell
# Abre PowerShell como Admin y ejecuta:

cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api

# Crear wwwroot
New-Item -ItemType Directory -Path wwwroot -Force

# Copiar archivos
Copy-Item -Path "..\Hanseatica.Api.Seida.Web\*" -Destination wwwroot -Recurse -Force

# Verificar
Get-ChildItem wwwroot
```

### 2. Editar Program.cs

**Abre:** `C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api\Program.cs`

**Busca:** 
```csharp
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Reemplaza por:**
```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();

// Fallback para SPA
app.MapFallbackToFile("index.html");

app.Run();
```

**Guarda el archivo (Ctrl+S)**

### 3. Compilar

```powershell
# En PowerShell, en la carpeta del API:
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
dotnet build
```

Deberías ver: ✅ `Build succeeded`

### 4. Ejecutar

```powershell
dotnet run
```

Deberías ver:
```
Now listening on: http://localhost:5000
```

### 5. Abrir navegador

```
http://localhost:5000
```

---

## ✅ Checklist

- [ ] Copiaste archivos web a `wwwroot`
- [ ] Editaste `Program.cs` (agregaste `UseStaticFiles()` y `MapFallbackToFile`)
- [ ] Compilaste con `dotnet build` (sin errores)
- [ ] Ejecutaste `dotnet run`
- [ ] Abriste `http://localhost:5000` en navegador
- [ ] Página carga sin errores
- [ ] Credenciales se guardan (recarga página)
- [ ] Puedes enviar archivos y consultar tickets

---

## 🧪 Prueba rápida en navegador

### Abre DevTools (F12 → Console)

```javascript
// Verifica que el API está conectado
fetch('/api/submissions/generate-transaction-id')
  .then(r => r.json())
  .then(d => console.log('✅ Conectado:', d))
```

Debería mostrar:
```
✅ Conectado: {transactionId: "20100010136-20260624-..."}
```

---

## 📊 Comparación de opciones

| Opción | Facilidad | Setup | Comando |
|--------|-----------|-------|---------|
| **1: wwwroot** | ⭐⭐⭐⭐⭐ Muy Fácil | 5 min | `dotnet run` (1 terminal) |
| **2: Visual Studio** | ⭐⭐⭐⭐ Fácil | 2 min | F5 en VS |
| **3: HTTP Server .NET** | ⭐⭐⭐ Normal | 10 min | `dotnet run WebServer.cs` |

---

## ✨ Mi recomendación

**Usa OPCIÓN 1:**
1. Copia archivos web a `wwwroot`
2. Edita `Program.cs` (3 líneas)
3. Ejecuta `dotnet run`
4. Accede a `http://localhost:5000`
5. ¡Listo! Todo funciona junto

**Ventajas:**
- ✅ Sin Python
- ✅ Sin IIS
- ✅ Sin dependencias extra
- ✅ API + Web en 1 proceso
- ✅ Sin problemas de CORS
- ✅ Perfecto para desarrollo y producción

---

## 💬 ¿Preguntas?

Si tienes problema en algún paso, revisa:
- **TESTING.md** - Troubleshooting
- **CONFIGURACION.md** - Más detalles técnicos
- **README.md** - Documentación general

---

**¡Adelante!** 🚀
