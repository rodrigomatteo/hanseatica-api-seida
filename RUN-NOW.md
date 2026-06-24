# ▶️ EJECUTA ESTO AHORA (1 minuto)

## 🎯 Objetivo
Servir la aplicación web sin Python, directamente desde .NET

---

## ⚡ Opción Más Rápida (Automática)

### En PowerShell, ejecuta este ÚNICO comando:

```powershell
cd C:\Hanseatica.Api.Seida
.\setup-web.ps1
```

**¿Qué hace?**
- ✅ Copia archivos web a `wwwroot` del API
- ✅ Edita automáticamente `Program.cs`
- ✅ Compila el proyecto
- ✅ Listo para ejecutar

**Salida esperada:**
```
✅ API encontrada
✅ Web encontrada
✅ Program.cs encontrado
✅ Carpeta wwwroot creada
✅ Archivo copiado: index.html
✅ Carpeta copiada: js
✅ Carpeta copiada: css
✅ Compilación exitosa
✅ Setup completado exitosamente!

📋 PRÓXIMOS PASOS:

1️⃣ Inicia el API:
    cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
    dotnet run

2️⃣ Abre en el navegador:
    http://localhost:5000

3️⃣ (Opcional) Ejecuta tests:
    cd ...\wwwroot
    .\test-api.ps1
```

---

## ▶️ Paso 2: Inicia el API

**En PowerShell:**
```powershell
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
dotnet run
```

**Espera a ver:**
```
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
```

---

## 🌐 Paso 3: Abre el navegador

```
http://localhost:5000
```

✅ **¡Debería cargar la aplicación web!**

---

## 🧪 Paso 4 (Opcional): Ejecuta tests

**En otra PowerShell:**
```powershell
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api\wwwroot
.\test-api.ps1
```

---

## ✅ Credenciales de prueba

```
RUC:         20100010136
Usuario SOL: TESTPAS1
Clave SOL:   TESTPAS1
Código Envío: 123456
```

**Click en cualquier campo → auto-guardan en localStorage**

---

## 🚀 Prueba rápida (2 minutos)

1. **Tab "Envíos"**
2. **Número Transacción:** Auto-generado ✓
3. **Archivo:** Selecciona cualquier ZIP (o crea uno)
4. **Click "+ Agregar"** → Aparece en tabla
5. **Click "Enviar"** → Muestra ticket

---

## 📊 ¿Qué sucede detrás de escenas?

```
Tu navegador (http://localhost:5000)
        ↓
        Sirve index.html desde wwwroot
        ↓
        app.js hace fetch a /api/submissions/...
        ↓
        .NET ruta los /api/* a los controladores
        ↓
        Respuesta vuelve a JavaScript
        ↓
        Muestra resultados en la interfaz
```

**Sin CORS, sin proxy, sin Python. Todo en .NET.**

---

## ❌ Si hay problema

### "Build failed"
```powershell
# Limpia y reintenta
cd C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api
rm -r bin obj
dotnet build
```

### "Cannot find index.html"
```powershell
# Verifica que se copió
Get-ChildItem C:\Hanseatica.Api.Seida\Hanseatica.Api.Seida.Api\wwwroot\index.html
```

### "API not responding"
- ✓ Verifica que `dotnet run` está en Terminal 1
- ✓ Abre `http://localhost:5000/health` en navegador
- ✓ Debería retornar "Healthy"

---

## 📚 Documentación

| Archivo | Usa si... |
|---------|-----------|
| **QUICKSTART-SIN-PYTHON.md** | Necesitas explicación paso a paso |
| **TESTING.md** | Quieres testing completo |
| **test-api.ps1** | Quieres tests automatizados |
| **README.md** | Quieres saber features de la web |
| **CONFIGURACION.md** | Quieres deploy a Azure |

---

## 🎯 Resumen

| Paso | Comando | Tiempo |
|------|---------|--------|
| 1 | `.\setup-web.ps1` | 30 seg |
| 2 | `dotnet run` (en API) | ∞ (hasta Ctrl+C) |
| 3 | Abre navegador | 5 seg |
| **Total** | | **~1 minuto** |

---

## ✨ Ventajas de esta setup

- ✅ **Sin Python** → Puro .NET
- ✅ **Sin IIS** → Integrado en .NET
- ✅ **Sin CORS** → Mismo dominio
- ✅ **Sin proxy** → Directo
- ✅ **Production-ready** → Funciona igual en Azure
- ✅ **Automático** → Script `setup-web.ps1`

---

## 🚀 **¡EJECUTA AHORA!**

```powershell
cd C:\Hanseatica.Api.Seida
.\setup-web.ps1
```

Luego:

```powershell
cd Hanseatica.Api.Seida.Api
dotnet run
```

Abre: `http://localhost:5000`

**¡Listo!** 🎉

---

**Cualquier duda → Lee QUICKSTART-SIN-PYTHON.md**
