# 🧪 Guía de Testing: Aplicación Web + Integración con Backend

## 📋 Tabla de Contenidos

1. [Testing Local (Desarrollo)](#-testing-local-desarrollo)
2. [Testing de Integración API ↔ Web](#-testing-de-integración-api--web)
3. [Casos de Prueba Manual](#-casos-de-prueba-manual)
4. [Testing Automatizado](#-testing-automatizado)
5. [Troubleshooting](#-troubleshooting)

---

## 🖥️ Testing Local (Desarrollo)

### Opción 1: Python HTTP Server + Proxy (RECOMENDADO)

**Ventaja:** Simula CORS y sirve la web como si estuviera en producción.

#### Paso 1: Crear archivo proxy.py

En la carpeta `Hanseatica.Api.Seida.Web/`, crea `proxy.py`:

```python
#!/usr/bin/env python3
"""
Proxy inverso para servir la aplicación web y hacer forward de llamadas API
Uso: python proxy.py
Accede a: http://localhost:8000
"""

import http.server
import socketserver
import urllib.request
import urllib.error
import os
from pathlib import Path

PORT = 8000
API_URL = "http://localhost:5000"  # Cambiar si tu API está en otro puerto

class ProxyHandler(http.server.SimpleHTTPRequestHandler):
    def translate_path(self, path):
        # Servir desde el directorio actual
        path = super().translate_path(path)
        return path

    def do_GET(self):
        if self.path.startswith('/api/'):
            self._proxy_api('GET')
        else:
            # Servir archivos estáticos
            if self.path == '/':
                self.path = '/index.html'
            super().do_GET()

    def do_POST(self):
        if self.path.startswith('/api/'):
            self._proxy_api('POST')
        else:
            super().do_POST()

    def _proxy_api(self, method):
        # Extraer path sin /api/
        api_path = self.path.replace('/api/', '/')
        api_url = f"{API_URL}/api{api_path}"
        
        # Leer body si existe
        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length) if content_length > 0 else None
        
        try:
            # Crear request
            headers = dict(self.headers)
            # Remover headers que no queremos pasar
            headers.pop('Host', None)
            headers.pop('Connection', None)
            
            req = urllib.request.Request(
                api_url,
                data=body,
                headers=headers,
                method=method
            )
            
            # Hacer request
            response = urllib.request.urlopen(req)
            
            # Enviar response
            self.send_response(response.status)
            for header, value in response.headers.items():
                self.send_header(header, value)
            self.send_header('Access-Control-Allow-Origin', '*')
            self.send_header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS')
            self.send_header('Access-Control-Allow-Headers', 'Content-Type')
            self.end_headers()
            
            self.wfile.write(response.read())
            
        except urllib.error.HTTPError as e:
            self.send_response(e.code)
            self.send_header('Content-Type', 'text/plain')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(e.read())
            
        except urllib.error.URLError as e:
            self.send_error(503, f"Backend unavailable: {e.reason}")
            return

    def do_OPTIONS(self):
        self.send_response(200)
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        self.end_headers()

    def log_message(self, format, *args):
        # Log más detallado
        if args[0].startswith('/api/'):
            print(f"[PROXY] {args[0]} → {API_URL}/api{args[0].replace('/api/', '/')}")
        else:
            super().log_message(format, *args)

if __name__ == '__main__':
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    
    with socketserver.TCPServer(("", PORT), ProxyHandler) as httpd:
        print(f"🌐 Web App: http://localhost:{PORT}")
        print(f"🔗 API Proxy: → {API_URL}")
        print(f"📁 Sirviendo desde: {os.getcwd()}")
        print(f"🛑 Presiona Ctrl+C para detener\n")
        
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\n✓ Servidor detenido")
```

#### Paso 2: Inicia el backend

**Terminal 1: Backend API**
```bash
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
dotnet run
```

Deberías ver:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
```

#### Paso 3: Inicia el proxy

**Terminal 2: Proxy + Web**
```bash
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web
python proxy.py
```

Deberías ver:
```
🌐 Web App: http://localhost:8000
🔗 API Proxy: → http://localhost:5000
📁 Sirviendo desde: C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web
🛑 Presiona Ctrl+C para detener
```

#### Paso 4: Abre la aplicación

En el navegador: **`http://localhost:8000`**

---

### Opción 2: Visual Studio Code (Live Server)

Si tienes VS Code con la extensión "Live Server":

```bash
# En VS Code
1. Abre la carpeta Hanseatica.Api.Seida.Web
2. Click derecho en index.html
3. "Open with Live Server"
```

⚠️ **Problema**: Live Server sirve en `http://localhost:5500` pero el API está en `localhost:5000` → CORS error.

**Solución:** Usa la Opción 1 (proxy.py) para evitar CORS.

---

### Opción 3: .NET CLI (Servir junto al API)

Copia `Hanseatica.Api.Seida.Web` a `wwwroot` del API:

```bash
# En PowerShell
Copy-Item -Path "C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web\*" `
          -Destination "C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api\wwwroot" `
          -Recurse -Force
```

Edita `Program.cs`:

```csharp
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");
```

Luego:

```bash
cd Hanseatica.Api.Seida.Api
dotnet run
```

Accede a: **`http://localhost:5000`**

✅ **Ventaja**: Sin CORS, todo en un solo proceso.

---

## 🔗 Testing de Integración API ↔ Web

### Test 1: Verificar conexión API

**En la consola del navegador (F12 → Console):**

```javascript
// Test 1: ¿Está accesible el API?
fetch('/api/submissions/generate-transaction-id')
  .then(r => r.json())
  .then(d => console.log('✅ API conectado:', d))
  .catch(e => console.error('❌ Error:', e))

// Output esperado:
// ✅ API conectado: {transactionId: "20100010136-20260624-..."}
```

Si ves `❌ Error: Failed to fetch`:
- ✓ Verifica que el API está corriendo (`http://localhost:5000`)
- ✓ Verifica CORS si están en dominios diferentes
- ✓ Abre DevTools → Network y mira la request fallida

---

### Test 2: Envío de archivo (Happy Path)

#### Paso 1: Rellenar credenciales

En la interfaz web:
```
RUC:         20100010136 (del appsettings.Development.json)
Usuario SOL: TESTPAS1
Clave SOL:   TESTPAS1
Código Envío: 123456
```

Click "Guardar" (se guarda en localStorage).

#### Paso 2: Crear archivo ZIP de prueba

```bash
# Opción A: Con PowerShell
New-Item -ItemType File -Path "C:\Temp\test.xml" -Value "<test>data</test>"
Compress-Archive -Path "C:\Temp\test.xml" -DestinationPath "C:\Temp\test.zip"

# Opción B: Descarga un ZIP de ejemplo
# De cualquier lugar
```

#### Paso 3: Enviar archivo

```
1. Tab "Envíos"
2. Número Transacción: Auto-generado (ej: 20100010136-20260624-103045-001)
3. Archivo: Selecciona test.zip
4. Click "+ Agregar" → Aparece en tabla
5. Click "Enviar"
```

**Resultado esperado:**

✅ **Success:**
```
Estado: "Completado"
Ticket: "123456" (o similar)
Sección "Resultados" muestra:
  ✓ Éxito
  Transacción: 20100010136-20260624-103045-001
  Archivo: test.zip
  Resultado: Ticket 123456
```

❌ **Error:**
```
Estado: "Error"
Toast: "Error procesando..."
Sección "Resultados" muestra:
  ✗ Error
  Error: [mensaje del API]
```

#### Paso 4: Verificar en logs

**Terminal API:**
```
info: Hanseatica.Api.Seida.Infrastructure.Services.SeidaClient[0]
      Submitting file to SEIDA service with transaction: 20100010136-20260624-103045-001
```

**Azure Portal (si está configurado):**
```
Application Insights → Logs →
traces | where message contains "Submitting" | top 10 by timestamp desc
```

---

### Test 3: Consulta de ticket (Happy Path)

#### Paso 1: Preparación

Necesitas un ticket válido. Puedes usar uno del test anterior o un dummy.

#### Paso 2: Consultar

```
1. Tab "Consultas"
2. Año: 2026
3. Número Ticket: 123456 (o el del test anterior)
4. Click "+ Agregar" → Aparece en tabla
5. Click "Consultar"
```

**Resultado esperado:**

✅ **Success:**
```
Estado: "Completado"
Sección "Resultados" muestra:
  ✓ Éxito
  Ticket: 2026-123456
  Status: "ACEPTADO"
  Descripción: "Documento aceptado por SUNAT"
```

❌ **Error (Ticket no existe):**
```
Estado: "Error"
Sección "Resultados" muestra:
  ✗ Error
  Error: "Ticket not found" (o similar)
```

---

### Test 4: Validaciones Frontend

#### Test 4a: RUC inválido

```
1. RUC: "12345" (menos de 11 dígitos)
2. Click "Enviar"
```

**Resultado esperado:**
```
Toast: "RUC debe tener 11 dígitos"
No se envía request
```

#### Test 4b: Archivo no seleccionado

```
1. Número Transacción: Auto-generado
2. Archivo: (vacío)
3. Click "+ Agregar"
```

**Resultado esperado:**
```
Toast: "Por favor selecciona un archivo ZIP"
```

#### Test 4c: Credenciales incompletas

```
1. RUC: (vacío)
2. Click "Enviar"
```

**Resultado esperado:**
```
Toast: "Por favor completa los datos de autenticación"
```

---

## ✅ Casos de Prueba Manual

### Test Suite Completo

| # | Caso de Prueba | Entrada | Resultado Esperado | Status |
|---|---|---|---|---|
| 1 | Cargar página | Abrir `http://localhost:8000` | Página carga sin errores | ⏳ |
| 2 | Datos persisten | Rellenar credenciales + reload | Credenciales permanecen | ⏳ |
| 3 | Generar transacción | Rellenar RUC | Nro transacción auto-generado | ⏳ |
| 4 | Validar RUC | RUC < 11 dígitos + Enviar | Error toast | ⏳ |
| 5 | Validar SOL | Usuario SOL > 15 caracteres + Enviar | Error toast | ⏳ |
| 6 | Agregar envío | Nro transacción + ZIP | Fila aparece en tabla | ⏳ |
| 7 | Seleccionar todos | Checkbox header envíos | Todas las filas seleccionadas | ⏳ |
| 8 | Eliminar envío | Seleccionar fila + Eliminar | Fila desaparece | ⏳ |
| 9 | Enviar archivo | Click "Enviar" | Resultado muestra ticket | ⏳ |
| 10 | Consultar ticket | Año + Ticket + Click "Consultar" | Resultado muestra status | ⏳ |
| 11 | Error API | API offline | Toast de error | ⏳ |
| 12 | CORS test | Web + API en dominios diferentes | Llamadas exitosas (con proxy) | ⏳ |
| 13 | LocalStorage | Guardar / Reload | Datos persisten | ⏳ |
| 14 | XSS prevention | Inyectar `<script>` en transacción | Script no ejecuta | ⏳ |
| 15 | Responsive | Redimensionar a 480px | Layout adaptable | ⏳ |

**Cómo ejecutar:**

Copia esta tabla a un editor (Excel, Google Sheets, etc.) y marca ⏳ con ✅ (pass) o ❌ (fail).

---

## 🤖 Testing Automatizado

### Test con cURL (desde terminal)

#### Test 1: Generar número de transacción

```bash
curl -X GET "http://localhost:5000/api/submissions/generate-transaction-id" \
  -H "Content-Type: application/json"
```

**Respuesta esperada:**
```json
{
  "transactionId": "20100010136-20260624-103045-001"
}
```

#### Test 2: Enviar archivo

```bash
# Crear un ZIP de prueba
echo "test content" > test.txt
powershell -Command "Compress-Archive -Path test.txt -DestinationPath test.zip"

# Enviar
curl -X POST "http://localhost:5000/api/submissions/submit-with-transaction?transactionNumber=20100010136-20260624-103045-001" \
  -H "Content-Type: application/octet-stream" \
  --data-binary "@test.zip"
```

**Respuesta esperada:**
```json
{
  "ticketNumber": "123456",
  "submissionYear": "2026",
  "issuerDocument": "20100010136",
  "errors": [],
  "warnings": []
}
```

#### Test 3: Consultar ticket

```bash
curl -X GET "http://localhost:5000/api/queries/query-status?year=2026&ticketNumber=123456" \
  -H "Content-Type: application/json"
```

**Respuesta esperada:**
```json
{
  "statusType": "ACEPTADO",
  "description": "Documento aceptado por SUNAT",
  "responseData": null
}
```

---

### Test con Postman

1. **Descargar Postman**: https://www.postman.com/downloads/
2. **Importar colección** (crear manualmente):

```
POST http://localhost:5000/api/submissions/submit-with-transaction
Query: transactionNumber=20100010136-20260624-103045-001
Body: (binary) test.zip
Headers: Content-Type: application/octet-stream
```

3. **Ejecutar y verificar response**

---

### Test Unitarios (Backend)

Los tests del backend ya existen. Para ejecutarlos:

```bash
cd C:\Hanseatica.Api.Seida
dotnet test --verbosity=normal
```

**Salida esperada:**
```
Test run for C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Tests\bin\Debug\net10.0\Hanseatica.Api.Seida.Tests.dll

Passed!  - Failed: 0, Passed: 188, Skipped: 0, Total: 188
```

---

## 🐛 Troubleshooting

### Problema 1: "CORS Error" / "Failed to fetch"

**Síntomas:**
```
GET http://localhost:5000/api/... 
No 'Access-Control-Allow-Origin' header
```

**Soluciones:**

**Opción A: Usar proxy.py** (Recomendado)
```bash
python proxy.py
# Accede a http://localhost:8000 (no http://localhost:5000)
```

**Opción B: Habilitar CORS en API**
```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");
```

**Opción C: Servir web desde wwwroot**
```bash
# Copy web files to wwwroot
cp -r Hanseatica.Api.Seida.Web/* Hanseatica.Api.Seida.Api/wwwroot/

# Edita Program.cs para servir estáticos
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
```

---

### Problema 2: "API not responding" / "503 Service Unavailable"

**Síntomas:**
```
Toast: "Failed to communicate with SEIDA service"
Console error: URLError: <urlopen error [Errno 111] Connection refused>
```

**Soluciones:**

1. ✓ Verifica que el API está corriendo:
```bash
dotnet run
# Deberías ver: "Now listening on: http://localhost:5000"
```

2. ✓ Verifica puerto correcto en `app.js`:
```javascript
const API_BASE_URL = '/api';  // o 'http://localhost:5000/api'
```

3. ✓ Verifica proxy.py en terminal 2:
```bash
python proxy.py
# Deberías ver: "API Proxy: → http://localhost:5000"
```

---

### Problema 3: "Cannot POST /api/submissions"

**Síntomas:**
```
Response: 404 Not Found
Toast: "HTTP 404"
```

**Soluciones:**

1. ✓ Verifica que el endpoint existe en API:
```bash
# En SoloCLI o Postman
GET http://localhost:5000/api/submissions/generate-transaction-id
```

2. ✓ Verifica ruta exacta:
   - Web envía a: `/api/submissions/submit-with-transaction`
   - API espera: `POST /api/submissions/submit-with-transaction`

3. ✓ Verifica URL en app.js:
```javascript
const response = await fetch(`${API_BASE_URL}/submissions/submit-with-transaction?...`);
```

---

### Problema 4: Credenciales no se guardan

**Síntomas:**
```
Rellenar RUC → Reload navegador → RUC vacío
```

**Soluciones:**

1. ✓ Verifica que localStorage está habilitado:
```javascript
// En consola del navegador (F12)
localStorage.setItem('test', 'value');
localStorage.getItem('test');  // Debería retornar 'value'
```

2. ✓ Si usas navegación privada/incógnita, localStorage está deshabilitado

3. ✓ Verifica que el evento "change" se dispara:
```javascript
// En app.js, línea ~85
[elements.ruc, ...].forEach(input => {
    input.addEventListener('change', saveToStorage);  // ✓ debe estar aquí
});
```

---

### Problema 5: Archivo no se envía

**Síntomas:**
```
Click "Enviar" → Nada ocurre
Console: No errores visibles
```

**Soluciones:**

1. ✓ Abre DevTools (F12 → Network) y mira si hay request:
   - Debe haber POST a `/api/submissions/submit-with-transaction`
   - Content-Type debe ser `application/octet-stream`

2. ✓ Verifica validaciones en app.js:
```javascript
if (!validateAuth()) return;  // ¿Tiene credenciales?
if (appState.envios.length === 0) return;  // ¿Hay archivos?
```

3. ✓ Verifica en console (F12 → Console):
```javascript
appState.envios  // Debería mostrar array con objetos
appState.isProcessing  // Debería ser false cuando termine
```

---

### Problema 6: "Cannot read property 'ruc' of undefined"

**Síntomas:**
```
Console error: TypeError: Cannot read property 'ruc' of undefined
```

**Causa:** Los elementos del DOM no se cargaron.

**Soluciones:**

1. ✓ Verifica que `<script src="js/app.js"></script>` está al final del `<body>`

2. ✓ Verifica que los IDs coinciden:
```html
<!-- index.html -->
<input type="text" id="ruc">
```

```javascript
// app.js
ruc: document.getElementById('ruc'),  // ✓ debe ser 'ruc'
```

---

## 📊 Checklist de Testing

Antes de deployar a producción, asegúrate de que todos estos tests pasaron:

- [ ] Página carga sin errores en DevTools (F12)
- [ ] Credenciales se guardan en localStorage
- [ ] Número de transacción se auto-genera al rellenar RUC
- [ ] Agregar envío agrega fila a tabla
- [ ] Eliminar envío elimina fila
- [ ] Enviar archivo hace POST a `/api/submissions/submit-with-transaction`
- [ ] Response muestra ticket en resultados
- [ ] Agregar consulta agrega fila a tabla
- [ ] Consultar hace GET a `/api/queries/query-status`
- [ ] Response muestra status en resultados
- [ ] Validaciones frontend funcionan (RUC, SOL, etc.)
- [ ] Toasts aparecen para success/error
- [ ] Layout es responsive en 480px, 768px, 1200px
- [ ] Console (F12) no muestra errores (excepto obsolescence del API)
- [ ] Network (F12) muestra requests correctas con status 200
- [ ] Logs aparecen en Application Insights (si está configurado)

---

## 🎯 Plan de Testing Recomendado

### Día 1: Development (localhost)

1. **Mañana:**
   - Inicia proxy.py y API
   - Abre `http://localhost:8000`
   - Ejecuta Test 1-5 (interfaz básica)

2. **Tarde:**
   - Ejecuta Test 6-10 (operaciones principales)
   - Ejecuta Test 11-15 (edge cases)

### Día 2: Integration

1. **Mañana:**
   - Tests con credenciales reales (si disponibles)
   - Verificar logs en Application Insights
   - Performance (timing de requests)

2. **Tarde:**
   - Tests en diferentes navegadores (Chrome, Firefox, Edge)
   - Tests en móvil (responsive)
   - Documentar resultados

### Día 3: Production Readiness

1. **Pre-deployment:**
   - Todos los tests pasados ✅
   - Logs configurados correctamente
   - CORS configurado para dominio de producción
   - Credenciales en Azure Key Vault

2. **Deployment:**
   - Publicar a Azure Static Web Apps (web)
   - Publicar a Azure App Service (API)
   - Verificar endpoints en producción

---

**¿Necesitas ayuda con algún test específico?** 🚀
