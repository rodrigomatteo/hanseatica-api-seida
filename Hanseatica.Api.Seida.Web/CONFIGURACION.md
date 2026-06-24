# Configuración de la Aplicación Web SEIDA

## 🔗 Conexión API ↔ Web

### Para Desarrollo Local

**Escenario:** API corriendo en `http://localhost:5000` y Web en `http://localhost:8000`

**Problema:** Las llamadas AJAX desde `localhost:8000` a `localhost:5000` fallarán por CORS.

**Solución 1: Proxy inverso con Python** (Recomendado para desarrollo)

```bash
# Instala si no tienes Python
# Desde la carpeta Hanseatica.Api.Seida.Web, crea un archivo proxy.py

import http.server
import socketserver
import urllib.request
import urllib.error
import json
from urllib.parse import urlparse, parse_qs

PORT = 8000
API_URL = "http://localhost:5000"

class ProxyHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith('/api/'):
            # Proxy a API
            api_path = self.path.replace('/api/', '/')
            try:
                response = urllib.request.urlopen(f"{API_URL}/api{api_path}")
                self.send_response(response.status)
                for header, value in response.headers.items():
                    self.send_header(header, value)
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(response.read())
            except urllib.error.URLError as e:
                self.send_error(503, str(e))
        else:
            # Servir archivos estáticos
            super().do_GET()

    def do_POST(self):
        if self.path.startswith('/api/'):
            content_length = int(self.headers.get('Content-Length', 0))
            body = self.rfile.read(content_length)
            
            api_path = self.path.replace('/api/', '/')
            try:
                req = urllib.request.Request(
                    f"{API_URL}/api{api_path}",
                    data=body,
                    headers={'Content-Type': self.headers.get('Content-Type')}
                )
                response = urllib.request.urlopen(req)
                self.send_response(response.status)
                for header, value in response.headers.items():
                    self.send_header(header, value)
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(response.read())
            except urllib.error.URLError as e:
                self.send_error(503, str(e))
        else:
            super().do_POST()

    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        super().end_headers()

    def do_OPTIONS(self):
        self.send_response(200)
        self.end_headers()

if __name__ == '__main__':
    with socketserver.TCPServer(("", PORT), ProxyHandler) as httpd:
        print(f"Sirviendo en http://localhost:{PORT}")
        print(f"Proxy a API en {API_URL}")
        httpd.serve_forever()
```

Luego ejecuta:
```bash
python proxy.py
```

Accede a `http://localhost:8000`

---

### Solución 2: Habilitar CORS en el API

Edita `Program.cs` del API:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Agregar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDevelopment", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            .WithOrigins("http://localhost:8000", "http://127.0.0.1:8000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
// ... resto del código ...

var app = builder.Build();

// Usar CORS
app.UseCors("AllowLocalDevelopment");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

### Solución 3: Servir ambas desde el mismo puerto

Copia la carpeta `Hanseatica.Api.Seida.Web` a `wwwroot` del API:

```
Hanseatica.Api.Seida.Api/
├── wwwroot/
│   ├── index.html
│   ├── js/
│   │   └── app.js
│   └── css/
│       └── styles.css
```

Edita `Program.cs`:

```csharp
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// Fallback para SPA
app.MapFallbackToFile("index.html");
```

Ahora todo funciona en `http://localhost:5000`

---

## 🚀 Para Producción (Azure)

### Opción A: Azure Static Web Apps + API en App Service

1. **Crear Azure Static Web App:**
   ```bash
   # Desde GitHub (recomendado)
   # Push el código a GitHub
   # En Azure Portal: Static Web Apps → Create → Conectar GitHub repo
   ```

2. **Configurar variable de entorno para API:**

   En `staticwebapp.config.json`:
   ```json
   {
     "routes": [
       {
         "route": "/api/*",
         "rewrite": "https://tu-api-app.azurewebsites.net/api/*"
       },
       {
         "route": "/*",
         "serve": "/index.html",
         "statusCode": 200
       }
     ]
   }
   ```

3. **O modificar `app.js` en tiempo de build:**
   
   ```bash
   # En GitHub Actions workflow
   - name: Configure API URL
     run: |
       sed -i "s|const API_BASE_URL = '/api'|const API_BASE_URL = 'https://tu-api-app.azurewebsites.net/api'|" Hanseatica.Api.Seida.Web/js/app.js
   ```

### Opción B: App Service único con web + API

1. **Crear App Service en Azure**
2. **Publicar la solución .NET con web incluida:**

   ```bash
   dotnet publish -c Release -o ./publish
   ```

3. **En la carpeta `publish/wwwroot`, colocar los archivos de web:**
   ```
   publish/
   └── wwwroot/
       ├── index.html
       ├── js/app.js
       └── css/styles.css
   ```

4. **Actualizar `Program.cs` para servir estáticos:**

   ```csharp
   app.UseStaticFiles();
   app.MapFallbackToFile("index.html");
   ```

---

## 🔐 Variables de Configuración por Ambiente

### Desarrollo (appsettings.Development.json)

```json
{
  "Serilog": { ... },
  "Seida": {
    "NumeroRUC": "20100118336",
    "UsuarioSOL": "TESTPAS1",
    "SolPassword": "TESTPAS1"
  }
}
```

### Producción (appsettings.json + Azure Key Vault)

```json
{
  "Seida": {
    "NumeroRUC": "@Microsoft.KeyVault(SecretUri=https://mykeyvault.vault.azure.net/secrets/seida-ruc/)",
    "UsuarioSOL": "@Microsoft.KeyVault(SecretUri=https://mykeyvault.vault.azure.net/secrets/seida-user/)",
    "SolPassword": "@Microsoft.KeyVault(SecretUri=https://mykeyvault.vault.azure.net/secrets/seida-pass/)"
  }
}
```

---

## 📊 Monitoreo y Logs

Con Serilog + Application Insights configurado, puedes:

1. **Ver logs en tiempo real:**
   ```
   Azure Portal → Application Insights → Live Metrics
   ```

2. **Consultar historial:**
   ```
   Azure Portal → Application Insights → Logs → 
   traces | where message contains "Envio" | sort by timestamp desc
   ```

3. **Alertas:**
   ```
   Azure Portal → Application Insights → Alerts →
   New Alert Rule → "Requests failed" > 5 in 5 minutes
   ```

---

## 🧪 Testing

### Test local: API + Web

```bash
# Terminal 1: API
cd Hanseatica.Api.Seida/Hanseatica.Api.Seida.Api
dotnet run

# Terminal 2: Web (con proxy)
cd Hanseatica.Api.Seida/Hanseatica.Api.Seida.Web
python proxy.py

# Terminal 3: Tests
cd Hanseatica.Api.Seida
dotnet test
```

Abre `http://localhost:8000` en el navegador.

---

## ✅ Checklist de Deployment

- [ ] CORS configurado (si API y Web están en dominios diferentes)
- [ ] Credentials en Azure Key Vault (NO en código)
- [ ] Serilog + Application Insights conectado
- [ ] HTTPS obligatorio
- [ ] Health checks activos (`/health` endpoint)
- [ ] Logs rotativos configurados
- [ ] Variables de entorno del ambiente apuntadas correctamente
- [ ] Todos los tests pasando
- [ ] Documentación actualizada

---

**Contacto:** rodgo.matteo@gmail.com
