# Cliente SEIDA para Web

Aplicación web moderna para interactuar con el API de SEIDA (Sistema de Envío Integrado de Documentos Aduaneros).

## 📋 Características

- ✅ **Envío de archivos ZIP**: Carga múltiples archivos y envía a SUNAT SEIDA
- ✅ **Consulta de tickets**: Verifica el estado de tickets previamente enviados
- ✅ **Interfaz moderna**: Diseño responsivo con UI/UX mejorada
- ✅ **Persistencia local**: Guarda credenciales en `localStorage` para evitar reingreso
- ✅ **Gestión de estado**: Manejo inteligente de filas (agregar, eliminar, seleccionar)
- ✅ **Resultados en tiempo real**: Visualización de respuestas exitosas y errores
- ✅ **Sin dependencias externas**: HTML/CSS/JavaScript puro, ninguna librería requerida

## 🚀 Estructura

```
Hanseatica.Api.Seida.Web/
├── index.html          # Página principal
├── js/
│   └── app.js          # Lógica de la aplicación
├── css/
│   └── styles.css      # Estilos responsivos
└── README.md           # Este archivo
```

## 🔧 Configuración

### API Base URL

La aplicación espera que el API esté disponible en `/api` (ruta relativa). 

**Para desarrollo local:**
- Si el API corre en `http://localhost:5000`, la aplicación debe servirse también desde `localhost` (mismo dominio)
- Ejemplo: API en `http://localhost:5000/api`, Web en `http://localhost:8000`

**Para producción (Azure):**
- Ambas aplicaciones (API y Web) deben estar en el mismo dominio o configurar CORS

```javascript
// En app.js, línea 2
const API_BASE_URL = '/api';  // Ruta relativa (recomendado)
// const API_BASE_URL = 'http://api.example.com/api';  // Ruta absoluta si es necesario
```

## 💻 Cómo servir la aplicación

### Opción 1: Python (Windows/Mac/Linux)

```bash
cd Hanseatica.Api.Seida.Web
python -m http.server 8000
```

Accede a: `http://localhost:8000`

### Opción 2: Node.js (si tienes instalado)

```bash
cd Hanseatica.Api.Seida.Web
npx http-server
```

Accede a: `http://localhost:8080`

### Opción 3: IIS (Windows Server)

1. Copia la carpeta `Hanseatica.Api.Seida.Web` a `C:\inetpub\wwwroot\seida-web\`
2. Abre IIS Manager y crea un nuevo sitio web:
   - **Ruta física**: `C:\inetpub\wwwroot\seida-web`
   - **Nombre de enlace**: `seida-web` (o tu dominio)
   - **Puerto**: 80 (HTTP) o 443 (HTTPS)
3. Accede a: `http://localhost/seida-web/` (si está en sitio compartido) o `http://tudominio.com` (si es sitio separado)

### Opción 4: Azure Static Web Apps (Recomendado)

1. Crear un repo con la carpeta `Hanseatica.Api.Seida.Web`
2. Conectar a Azure Static Web Apps
3. Configurar ruta relativa para `/api` o variable de entorno en GitHub Actions

```yaml
# En el workflow de GitHub Actions
- name: Set API URL
  run: |
    sed -i "s|const API_BASE_URL = '/api'|const API_BASE_URL = 'https://api.example.com/api'|" Hanseatica.Api.Seida.Web/js/app.js
```

### Opción 5: Mismo servidor que el API (ASP.NET)

Copiar la carpeta `Hanseatica.Api.Seida.Web` a `wwwroot` del API:

```
Hanseatica.Api.Seida.Api/
├── wwwroot/
│   ├── index.html
│   ├── js/app.js
│   └── css/styles.css
├── Program.cs
└── ...
```

Y actualizar `Program.cs`:

```csharp
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");  // Para SPA routing
```

## 📱 Interfaz de usuario

### Datos de Autenticación
Campos requeridos para conectar con SEIDA:
- **RUC**: Número de RUC (11 dígitos)
- **Usuario SOL**: Usuario SOL del RUC (máx 15 caracteres)
- **Clave SOL**: Contraseña SOL (campo protegido)
- **Código de Envío**: Código de envío asignado

*Nota: Las credenciales se guardan en `localStorage` del navegador para comodidad.*

### Datos Referenciales
Información del sistema (simulada):
- **Dirección IP**: IP de la interfaz de red
- **Dirección MAC**: MAC de la interfaz de red

Botón "↻ Actualizar" para refrescar estos datos.

### Tabs: Envíos y Consultas

#### Envíos
1. Ingresa número de transacción (auto-generado con RUC + fecha/hora)
2. Selecciona un archivo ZIP
3. Click "Agregar" para agregarlo a la tabla
4. Repite para múltiples archivos
5. Click "Enviar" para procesar todos
6. Ver resultados con tickets asignados

#### Consultas
1. Selecciona año de envío original
2. Ingresa número de ticket (del acuse recibido)
3. Click "Agregar" para agregarlo a la tabla
4. Repite para múltiples tickets
5. Click "Consultar" para obtener estado
6. Ver resultados con estado y descripción

## 🔐 Seguridad

⚠️ **Advertencias:**
- Las credenciales se guardan en `localStorage` — NO es seguro para producción en navegadores públicos
- Para producción, considera:
  - Usar sesiones con tokens JWT en el servidor
  - Implementar HTTPS obligatoriamente
  - Agregar autenticación multi-factor (MFA)
  - Usar variables de entorno para credenciales sensibles

## 🛠️ Desarrollo

### Estructura del código JavaScript (`app.js`)

```javascript
// Configuración (línea 1)
const API_BASE_URL = '/api';

// Estado global (línea 7)
let appState = { auth: {...}, envios: [], consultas: [], ... };

// Elemento del DOM (línea 23)
const elements = { ruc: ..., btnEnviar: ..., ... };

// Inicialización (línea 74)
document.addEventListener('DOMContentLoaded', () => {...});

// Event listeners (línea 80)
function setupEventListeners() {...}

// Envios (línea 115)
function addEnvio() {...}
function removeSelectedEnvios() {...}
function submitEnvios() {...}
async function submitFile(envio) {...}

// Consultas (línea 226)
function addConsulta() {...}
function removeSelectedConsultas() {...}
function submitConsultas() {...}
async function queryStatus(consulta) {...}

// Utilidades (línea 350)
function showLoading(show) {...}
function showToast(message, type) {...}
function escapeHtml(text) {...}
```

### Modificar variables

Para cambiar colores, tamaños o comportamiento, edita `css/styles.css`:

```css
/* Variables en la sección root */
:root {
    --primary: #0066cc;           /* Color principal */
    --primary-dark: #004499;
    --success: #28a745;
    --danger: #dc3545;
    /* ... más variables ... */
}
```

## 📋 Endpoints esperados del API

La aplicación consume estos endpoints:

```http
POST /api/submissions/submit-with-transaction?transactionNumber={id}
Content-Type: application/octet-stream

[binary file content]

Response: { ticketNumber, errors[], warnings[] }

---

GET /api/queries/query-status?year={yyyy}&ticketNumber={number}

Response: { statusType, description, responseData }
```

## 🐛 Troubleshooting

### "Error de CORS"
Asegúrate de que API y Web están en el mismo dominio o que el API tiene CORS habilitado.

### "La aplicación no carga"
Verifica que:
1. El archivo `index.html` está siendo servido correctamente
2. Los archivos `js/app.js` y `css/styles.css` están en las rutas correctas
3. No hay errores en la consola del navegador (F12 → Console)

### "Los datos no se guardan"
Comprueba que el navegador tiene `localStorage` habilitado (privado/incógnito lo deshabilita).

## 📞 Soporte

Para reportar bugs o sugerencias, crea un issue en el repositorio o contacta al equipo de desarrollo.

---

**Última actualización:** Junio 2026  
**Versión:** 1.0  
**Tecnologías:** HTML5, CSS3, JavaScript (vanilla, sin frameworks)
