# 🚀 Quick Start: Testing Aplicación Web + Backend

## ⏱️ 5 minutos para empezar

### 1️⃣ **Terminal 1: Inicia el Backend (API)**

```bash
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
dotnet run
```

✅ Cuando veas:
```
Now listening on: http://localhost:5000
```

---

### 2️⃣ **Terminal 2: Inicia el Proxy + Web**

```bash
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web
python proxy.py
```

✅ Cuando veas:
```
🌐 Web App: http://localhost:8000
🔗 API Proxy: → http://localhost:5000
```

---

### 3️⃣ **Abre el navegador**

```
http://localhost:8000
```

---

## 🧪 Prueba Rápida (2 minutos)

### Paso 1: Rellenar credenciales

```
RUC:         20100010136
Usuario SOL: TESTPAS1
Clave SOL:   TESTPAS1
Código Envío: 123456
```

Press Tab o click en otro campo para auto-guardar.

---

### Paso 2: Enviar archivo

1. **Tab "Envíos"**
2. **Número Transacción:** Auto-generado ✓
3. **Archivo:** Crea un ZIP (ve abajo ↓) o descarga uno
4. **Click "Agregar"** → Aparece en tabla
5. **Click "Enviar"** → Procesa

✅ Resultado esperado:
```
✓ Éxito
Ticket: 123456
```

---

### Paso 3: Consultar ticket

1. **Tab "Consultas"**
2. **Año:** 2026
3. **Número Ticket:** El que salió en Paso 2 (ej: 123456)
4. **Click "Agregar"** → Aparece en tabla
5. **Click "Consultar"** → Procesa

✅ Resultado esperado:
```
✓ Éxito
Status: ACEPTADO
```

---

## 📦 Crear archivo ZIP de prueba

### PowerShell

```powershell
# Crear contenido
"<test>data</test>" | Out-File -FilePath "C:\test.xml" -Encoding UTF8

# Crear ZIP
Compress-Archive -Path "C:\test.xml" -DestinationPath "C:\test.zip"

# Selecciona "C:\test.zip" en la interfaz web
```

---

## 🧪 Testing Automatizado

### Script PowerShell (Prueba todos los endpoints)

```bash
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Web
.\test-api.ps1
```

Verás:
```
✅ TEST 1: Conexión a API
✅ TEST 2: Generar Número de Transacción
✅ TEST 3: Envío de Archivo
✅ TEST 4: Consulta de Ticket
✅ TEST 5: Verificar Endpoints
✅ TEST 6: CORS Preflight

🎉 ¡TODOS LOS TESTS PASARON!
```

---

## 🔍 Verificar en DevTools

### Abrir DevTools (F12)

#### Console
```javascript
// Verificar conexión
fetch('/api/submissions/generate-transaction-id')
  .then(r => r.json())
  .then(d => console.log('✅ Conectado:', d))
  .catch(e => console.error('❌ Error:', e))
```

#### Network
1. Abre pestaña **Network**
2. Click "Enviar" en la web
3. Mira las requests:
   - POST `/api/submissions/submit-with-transaction` → Status 200 ✓
   - GET `/api/queries/query-status` → Status 200 ✓

#### Application
- **Storage** → **Local Storage**
- Busca `seida_auth`
- Debería contener: `{"ruc":"20100010136",...}`

---

## 📊 Verificar Logs

### En el Terminal del Backend (Terminal 1)

Deberías ver:
```
info: Hanseatica.Api.Seida.Infrastructure.Services.SeidaClient[0]
      Submitting file to SEIDA service with transaction: 20100010136-20260624-103045-001
      ThreadId: 5, MachineName: DESKTOP-ABC123
```

---

## ❌ Si hay problemas

### "CORS Error" / "Failed to fetch"

✅ **Solución:** Ya lo debería evitar `proxy.py`, pero si no:

1. Verifica que `proxy.py` está corriendo en Terminal 2
2. Accede a `http://localhost:8000` (NO `localhost:5000`)
3. En DevTools, Console debería estar limpia

---

### "API not responding"

✅ **Solución:**

1. Verifica que Terminal 1 (Backend) está corriendo
2. Ve a `http://localhost:5000/health`
3. Debería responder con 200 OK

---

### Archivo no se envía

✅ **Solución:**

1. Abre DevTools (F12)
2. Ve a **Network**
3. Click "Enviar"
4. Busca POST request a `/api/submissions/submit-with-transaction`
5. Verifica:
   - Status sea 200 (no 404, 500, etc.)
   - Content-Type sea `application/octet-stream`

---

## 📚 Documentación Completa

| Documento | Para quién | Qué contiene |
|---|---|---|
| **TESTING.md** | QA / Developers | Guía completa: 15 casos de prueba, troubleshooting |
| **README.md** | Users / Admins | Cómo usar la web, características |
| **CONFIGURACION.md** | DevOps | Integración API ↔ Web, deployment, CORS |
| **test-api.ps1** | Developers | Script automático para probar endpoints |

---

## ✅ Checklist de Validación

- [ ] Terminal 1 (API) está corriendo en `http://localhost:5000`
- [ ] Terminal 2 (Proxy) está corriendo en `http://localhost:8000`
- [ ] Página carga sin errores
- [ ] Credenciales se guardan (recarga la página)
- [ ] Agregar envío agrega fila
- [ ] Click "Enviar" hace POST y muestra resultado
- [ ] Agregar consulta agrega fila
- [ ] Click "Consultar" hace GET y muestra resultado
- [ ] DevTools Network muestra requests con Status 200
- [ ] DevTools Console está limpia (sin errores)

---

## 🎯 Próximos Pasos

### Para desarrollar más:

```bash
# Terminal 3: Lanza tests del backend
cd C:\Hanseatica.Api.Seida
dotnet test --watch
```

---

### Para desplegar a Azure:

Ve a: `Hanseatica.Api.Seida.Web/CONFIGURACION.md` → Sección "Para Producción (Azure)"

---

## 💬 Comandos Rápidos

```bash
# Inicia API
cd Hanseatica.Api.Seida.Api && dotnet run

# Inicia Proxy + Web
cd Hanseatica.Api.Seida.Web && python proxy.py

# Tests automatizados
cd Hanseatica.Api.Seida.Web && .\test-api.ps1

# Tests del backend
cd Hanseatica.Api.Seida && dotnet test
```

---

**¿Listo?** Abre `http://localhost:8000` y ¡comienza a probar! 🚀

