# Gestión de Archivos en WinClienteSEIDA - Análisis Completo

## 1. ESTRUCTURA DE DIRECTORIOS

### Directorios Principales Creados al Iniciar
La aplicación crea automáticamente 4 directorios de trabajo al cargar (en el directorio de ejecución):

```
{AppPath}/
├── origen_documento/          # Almacena archivos ZIP temporales a enviar
├── destino_acuse/             # Almacena acuses de recibo (XML) después del envío
├── destino_respuesta/         # Almacena respuestas de consultas (XML/ZIP)
└── log/                        # Contiene logs de eventos
    └── BitacoraEventos.txt    # Archivo único de trazabilidad
```

**Ubicación de creación:** `Core.cs:VerifieDirectories()`
**Ubicación de uso:** `FrmMain.cs:FrmMain()` constructor → línea 54

```csharp
Logic.Core.VerifieDirectories(m_pathApp);
// m_pathApp = Application.StartupPath (e.g., "C:\WinClienteSEIDA\bin\Debug")
```

---

## 2. GESTIÓN DEL LOG DE EVENTOS - BitacoraEventos.txt

### Propósito Funcional
Archivo único que centraliza **TODA** la trazabilidad de la aplicación:
- Envíos de archivos
- Consultas de tickets
- Errores de conexión
- Intentos de autenticación
- Estados de proxy
- Información de red (IP, MAC)

### Detalles Técnicos

**Ubicación:** `{AppPath}/log/BitacoraEventos.txt`

**Modo de escritura:**
- Abierto en modo `FileMode.OpenOrCreate` (crear si no existe, abrir si existe)
- Acceso: `FileAccess.ReadWrite`
- Compartido: `FileShare.ReadWrite` (múltiples procesos pueden leer/escribir)
- Posición: Siempre al final del archivo con `Seek(0, SeekOrigin.End)`

**Código de inicialización:**
```csharp
// FrmMain.cs:CreateListener()
if (File.Exists(m_pathApp + "\\" + Logic.Core.DIRECTORIO_LOGGER + "\\" + Logic.Core.TRACE_LOG_FILENAME))
    m_traceLog = new System.IO.FileStream(
        path: m_pathApp + "\\log\\BitacoraEventos.txt",
        mode: System.IO.FileMode.Open,
        access: FileAccess.ReadWrite,
        share: FileShare.ReadWrite);
else
    m_traceLog = new System.IO.FileStream(
        path: m_pathApp + "\\log\\BitacoraEventos.txt",
        mode: System.IO.FileMode.OpenOrCreate,
        access: FileAccess.ReadWrite,
        share: FileShare.ReadWrite);

m_traceLog.Seek(0, SeekOrigin.End);
m_listenerLog = new TextWriterTraceListener(m_traceLog);
Trace.Listeners.Add(m_listenerLog);
```

**Escritura de eventos:**
- Método: `Core.TraceLogApp(string message)`
- Formato: `[HH:MM:SS] {message}`
- Tiempo: `DateTime.Now.ToLongTimeString()` + mensaje

```csharp
// Core.cs
public static void TraceLogApp(string message)
{
    Trace.WriteLine(DateTime.Now.ToLongTimeString() + " " + message);
}
```

**Actualización en tiempo real:**
- Se ejecuta `m_listenerLog.Flush()` y `m_traceLog.Flush()` después de cada operación de envío/consulta
- Ubicación: Fin de métodos `SendFiles()` y `SendQuerys()` en FrmMain.cs

**Eventos registrados:**
1. **Inicio/Cierre de aplicación:**
   - `Ruta de Ejecucion: {path}`
   - `NombrePC: {hostname}`
   - `IP: {ipv4}`
   - `MAC: {mac_address}`

2. **Por cada operación de envío:**
   - `Enviando Hilo({correlativo}) | Nro.Transaccion:{id} | Archivo: {filename}`
   - `Respuesta Hilo({correlativo}) | Ticket: {ticketEnvio} | acuse:{acuse_file_path}`

3. **Por cada consulta:**
   - `Consulta Hilo({correlativo}) | Año:{year} | Ticket: {ticket_number}`
   - `Respuesta Hilo({correlativo}) | Ticket:{ticket} | Archivo: {response_file_path}`

4. **Errores:**
   - `Error OnStartSend : {fault_message}`
   - `Error OnStartQuery : {fault_message}`
   - `Error en formato, el segundo parametro debe ser un archivo que exista: {line}`

### Cierre de Aplicación
```csharp
// FrmMain.cs:CloseListener()
private void CloseListener()
{
    m_traceLog.Seek(0, SeekOrigin.End);
    m_listenerLog.Flush();
    m_traceLog.Flush();
    m_listenerLog.Close();
    m_listenerLog.Dispose();
    m_traceLog.Close();
    m_traceLog.Dispose();
}
```

---

## 3. GESTIÓN DE ARCHIVOS DE ENVÍO Y RESPUESTA

### Flujo de Archivos ZIP (Envíos)

```
Usuario selecciona .ZIP
    ↓
Se agrega a m_dtEnvios (DataTable)
    ↓
Botón "Enviar" → SendFiles()
    ↓
Para cada archivo en m_dtEnvios:
  - Crear ThreadProxy (tipo=1 para envío)
  - Leer archivo: File.ReadAllBytes(ruta_zip)
  - Serializar a SOAP → recibirArchivo(numeroTransaccion, byte[] zipData)
  - Recibir acuseRecibo
    ↓
Guardar acuse como XML en destino_acuse/
    ↓
Actualizar m_dtEnvios.Estado = "Completado"
    ↓
Log: "Respuesta Hilo(...) | Ticket: {ticket} | acuse:{archivo}"
```

**Generación de nombre de acuse:**
```csharp
// Core.cs
public static string GenerateNameAcuse(string anno, string ticket)
{
    return "seida_acuse_" + anno + "_" + ticket.Trim() + ".xml";
}
// Resultado: "seida_acuse_2014_123456.xml"
```

**Guardado del acuse:**
```csharp
// ThreadProxy.cs:OnStartSend()
Logic.WS.acuseRecibo oTicket = (Logic.WS.acuseRecibo)valueResult;
string fileName = this.directorioSave + "\\" + Logic.Core.GenerateNameAcuse(
    oTicket.anhoEnvio, 
    oTicket.ticketEnvio
);
XmlSerializer x = new XmlSerializer(typeof(Logic.WS.acuseRecibo));
TextWriter writer = new StreamWriter(fileName);
x.Serialize(writer, valueResult);  // Serializar objeto SOAP a XML
writer.Close();
```

### Flujo de Archivos de Consulta (Respuestas)

```
Usuario ingresa ticket y año
    ↓
Se agrega a m_dtConsultas (DataTable)
    ↓
Botón "Consultar" → SendQuerys()
    ↓
Para cada consulta en m_dtConsultas:
  - Crear ThreadProxy (tipo=2 para consulta)
  - Armar query XML: <consulta><tipo>1</tipo><parametros>...
  - Llamar: realizarConsulta(queryXml, out byte[] fileBytes)
  - Recibir respuestaConsultaBean + fileBytes
    ↓
Si fileBytes ≠ null:
  - Generar nombre único: "seida_respuesta_YYYY_YYYYMMDDHHMMSS_UUID_ticket.xml"
  - Guardar en destino_respuesta/
  - Actualizar m_dtConsultas.ArchivoRecibido = ruta_archivo
    ↓
Log: "Respuesta Hilo(...) | Ticket:{ticket} | Archivo: {ruta}"
```

**Generación de nombre de respuesta:**
```csharp
// Core.cs
public static string GenerateNameResponse(string anno, string ticket)
{
    return "seida_respuesta_" + anno + "_" + NameUnique() + "_" + ticket.Trim() + ".xml";
}

public static string NameUnique()
{
    return DateTime.Now.Year.ToString() + 
           DateTime.Now.Month.ToString().PadLeft(2, '0') + 
           DateTime.Now.Day.ToString().PadLeft(2, '0') + 
           DateTime.Now.Hour.ToString().PadLeft(2, '0') + 
           DateTime.Now.Minute.ToString().PadLeft(2, '0') + 
           DateTime.Now.Second.ToString().PadLeft(2, '0');
}
// Resultado: "seida_respuesta_2014_20140614160707_a1b2c3d4e5f6_123456.xml"
```

**Guardado de respuesta:**
```csharp
// ThreadProxy.cs:OnStartQuery()
byte[] fileBytes;
Logic.WS.respuestaConsultaBean valueResult = oProxy.EndrealizarConsulta(
    result, 
    out fileBytes
);
if (fileBytes != null)
{
    string fileName = this.directorioSave + "\\" + Logic.Core.GenerateNameResponse(
        this.annoConsulta, 
        this.ticketConsulta
    );
    this.nombreRespuesta = fileName;
    File.WriteAllBytes(@fileName, fileBytes);  // Guardar bytes directamente
}
```

---

## 4. GESTIÓN DE BASE DE DATOS - DataTables (En Memoria)

### Nota Importante: NO hay base de datos SQL
La aplicación **NO usa SQL Server, Oracle, o ninguna BD relacional**. Todo se maneja en memoria con DataTables de .NET Framework.

### DataTable 1: m_dtEnvios (Envíos de Archivos)

**Propósito:** Almacenar lista de archivos ZIP a enviar a SUNAT

**Estructura de columnas:**
```csharp
// Core.cs:CreateModelSend()
DataTable table = new DataTable();
table.Columns.Add("EnvioId", typeof(string));          // ID único (YYYYMMDDHHMMSS_index)
table.Columns.Add("NumeroTransaccion", typeof(string)); // ID transacción (ingresado por usuario)
table.Columns.Add("NombreArchivo", typeof(string));    // Ruta completa del .ZIP
table.Columns.Add("Estado", typeof(string));           // "Pendiente" o respuesta
```

**Ejemplo de datos:**
```
| EnvioId                    | NumeroTransaccion | NombreArchivo                           | Estado    |
|----------------------------|-------------------|----------------------------------------|-----------|
| 20140614160707_1           | 0101              | C:\...\destino_acuse\archivo1.zip      | Completado|
| 20140614160708_2           | 0102              | C:\...\destino_acuse\archivo2.zip      | Completado|
```

**Ciclo de vida:**
1. **Creación:** `FrmMain.cs` constructor → `m_dtEnvios = Logic.Core.CreateModelSend()`
2. **Poblado:** 
   - Opción A: Usuario selecciona archivo ZIP individual
     ```csharp
     // FrmMain.cs:EvtAddFile()
     m_dtEnvios.Rows.Add(
         Logic.Core.NameUnique(),        // EnvioId
         this.txtNroTransaccion.Text,    // NumeroTransaccion
         this.txtArchivoEnvio.Text,      // NombreArchivo (ruta)
         "Pendiente"                     // Estado inicial
     );
     ```
   - Opción B: Usuario carga archivo TXT con lote de envíos
     ```csharp
     // FrmMain.cs:EvtAddFile() + Core.cs:AddSendFromFile()
     // Formato archivo TXT: 0101,C:\path\archivo.zip
     int error = Logic.Core.AddSendFromFile("ENVIO", ref m_dtEnvios, txtArchivoEnvio.Text, "Pendiente");
     ```

3. **Envío:** `SendFiles()` → crea threads para procesar cada fila
   ```csharp
   for (int i = 0; i < m_dtEnvios.Rows.Count; i++)
   {
       Logic.ThreadProxy otp = new Logic.ThreadProxy("Task-" + i.ToString(), 1);
       otp.NumeroTransaccion = m_dtEnvios.Rows[i][1].ToString();  // Leer NumeroTransaccion
       otp.NombreArchivo = m_dtEnvios.Rows[i][2].ToString();      // Leer NombreArchivo
       otp.Run();  // Ejecutar en thread separado
   }
   ```

4. **Limpieza:** Después de procesar todos los envíos
   ```csharp
   // FrmMain.cs:SendFiles()
   m_dtEnvios.Rows.Clear();
   this.grdEnvios.DataSource = m_dtEnvios;
   this.grdEnvios.Refresh();
   ```

**UI vinculada:** DataGridView `grdEnvios` (FrmMain.Designer.cs)

---

### DataTable 2: m_dtConsultas (Consultas de Tickets)

**Propósito:** Almacenar lista de tickets a consultar para obtener estado

**Estructura de columnas:**
```csharp
// Core.cs:CreateModelQuery()
DataTable table = new DataTable();
table.Columns.Add("ConsultaId", typeof(string));     // ID único (YYYYMMDDHHMMSS_index)
table.Columns.Add("Anno", typeof(string));           // Año de envío (YYYY)
table.Columns.Add("NumeroTicket", typeof(string));   // Número de ticket (de acuse recibido)
table.Columns.Add("ArchivoRecibido", typeof(string)); // Ruta del archivo de respuesta
```

**Ejemplo de datos:**
```
| ConsultaId          | Anno | NumeroTicket | ArchivoRecibido                                      |
|---------------------|------|--------------|------------------------------------------------------|
| 20140614160707_1    | 2014 | 123456       | C:\...\destino_respuesta\seida_respuesta_2014_...xml |
| 20140614160708_2    | 2014 | 123457       | C:\...\destino_respuesta\seida_respuesta_2014_...xml |
```

**Ciclo de vida:**
1. **Creación:** `FrmMain.cs` constructor → `m_dtConsultas = Logic.Core.CreateModelQuery()`
2. **Poblado:**
   - Opción A: Usuario ingresa ticket individual
     ```csharp
     // FrmMain.cs:EvtAddTicket()
     if (long.TryParse(this.txtNroTicket.Text.Trim(), out intParsed))
     {
         m_dtConsultas.Rows.Add(
             Logic.Core.NameUnique(),   // ConsultaId
             this.picAno.Text,          // Anno
             this.txtNroTicket.Text,    // NumeroTicket
             ""                         // ArchivoRecibido (inicialmente vacío)
         );
     }
     ```
   - Opción B: Usuario carga archivo TXT con lote de tickets
     ```csharp
     // FrmMain.cs:EvtSelectTickets() + Core.cs:AddSendFromFile()
     // Formato archivo TXT: 2014,123456
     int error = Logic.Core.AddSendFromFile("CONSULTA", ref m_dtConsultas, openfile.FileName, "");
     ```

3. **Consulta:** `SendQuerys()` → crea threads para procesar cada fila
   ```csharp
   for (int i = 0; i < m_dtConsultas.Rows.Count; i++)
   {
       Logic.ThreadProxy otp = new Logic.ThreadProxy("Task-" + i.ToString(), 2);
       otp.AnnoConsulta = m_dtConsultas.Rows[i][1].ToString();      // Leer Anno
       otp.TicketConsulta = m_dtConsultas.Rows[i][2].ToString();    // Leer NumeroTicket
       otp.Run();  // Ejecutar en thread separado
   }
   ```

4. **Actualización:** Después de recibir respuestas
   ```csharp
   // FrmMain.cs:SendQuerys() - línea ~360
   for (int j = 0; j < objectsThread.Count; j++)
   {
       Logic.ThreadProxy otp = (Logic.ThreadProxy)objectsThread[j];
       for (int k = 0; k < m_dtConsultas.Rows.Count; k++)
       {
           if (m_dtConsultas.Rows[k][0].ToString() == otp.IdProceso)
           {
               m_dtConsultas.Rows[k]["ArchivoRecibido"] = otp.NombreRespuesta;
               // Actualizar la columna con la ruta del archivo descargado
               break;
           }
       }
   }
   ```

5. **Limpieza:** Manualmente por usuario
   ```csharp
   // FrmMain.cs:pctLimpiarLista_Click()
   private void pctLimpiarLista_Click(object sender, EventArgs e)
   {
       m_dtConsultas.Rows.Clear();
       this.grdConsultas.DataSource = m_dtConsultas;
       this.grdConsultas.Refresh();
   }
   ```

**UI vinculada:** DataGridView `grdConsultas` (FrmMain.Designer.cs)

---

### Sincronización DataTable ↔ UI

Las DataTables están **vinculadas directamente a DataGridView**:

```csharp
// Siempre que cambia la DataTable, se actualiza la UI
this.grdEnvios.DataSource = m_dtEnvios;
this.grdEnvios.Refresh();

this.grdConsultas.DataSource = m_dtConsultas;
this.grdConsultas.Refresh();
```

**Búsqueda de filas en DataTable:**
```csharp
// Core.cs:GetIndexRowFromDataTable()
public static int GetIndexRowFromDataTable(DataTable findTable, string valueFind)
{
    int index = -1;
    for (int i = 0; i < findTable.Rows.Count; i++)
    {
        if (valueFind.Equals(findTable.Rows[i][0].ToString()))
        {
            index = i;
            break;
        }
    }
    return index;
}
```

---

## 5. LECTURA Y CARGA DE ARCHIVOS EN LOTE

### Formato de Archivo TXT para Envíos

**Ubicación en UI:** FrmMain.cs → botón "Agregar Archivo"

**Formato:**
```
{NumeroTransaccion},{RutaCompleta.zip}
{NumeroTransaccion},{RutaCompleta.zip}
```

**Ejemplo:**
```
0101,C:\WinClienteSEIDA\Test\archivos_de_sunat\0101.zip
0102,C:\WinClienteSEIDA\Test\archivos_de_sunat\0102.zip
```

**Procesamiento:**
```csharp
// Core.cs:AddSendFromFile()
public static int AddSendFromFile(string type, ref DataTable dt, string fileName, string response)
{
    string line = "";
    int index = 0;
    int error = 0;
    StreamReader swSource = new StreamReader(fileName);
    try
    {
        while (true)
        {
            line = swSource.ReadLine();
            if (line == null) break;
            
            if (line.IndexOf(",") > 0)  // Validar separador
            {
                string parameter1 = line.Substring(0, line.IndexOf(","));
                string parameter2 = line.Substring(line.IndexOf(",") + 1);
                
                if ("ENVIO" == type)
                {
                    if (File.Exists(parameter2))  // Validar que el archivo existe
                    {
                        index++;
                        dt.Rows.Add(
                            Logic.Core.NameUnique() + "_" + index.ToString(),
                            parameter1,      // NumeroTransaccion
                            parameter2,      // NombreArchivo
                            response         // Estado
                        );
                    }
                    else
                    {
                        TraceLogApp("Error: archivo no existe: " + line);
                        error++;
                    }
                }
                // ... más validaciones ...
            }
            else
            {
                TraceLogApp("Error: se esperaba separador coma en línea: " + line);
                error++;
            }
        }
    }
    catch { }
    swSource.Close();
    return error;  // Retorna número de errores encontrados
}
```

**Retorno:**
- Si `error > 0`, muestra MessageBox advirtiendo de líneas descartadas
- Las líneas válidas se agregan a la DataTable
- Las líneas inválidas se registran en BitacoraEventos.txt

### Formato de Archivo TXT para Consultas

**Ubicación en UI:** FrmMain.cs → botón "Agregar Tickets"

**Formato:**
```
{Año},{NumeroTicket}
{Año},{NumeroTicket}
```

**Ejemplo:**
```
2014,123456
2014,123457
```

**Procesamiento:** (mismo código `AddSendFromFile()` con `type="CONSULTA"`)

```csharp
if ("CONSULTA" == type)
{
    long intParsed;
    if (long.TryParse(parameter1, out intParsed))        // Validar año es número
    {
        if (long.TryParse(parameter2, out intParsed))    // Validar ticket es número
        {
            index++;
            dt.Rows.Add(
                Logic.Core.NameUnique() + "_" + index.ToString(),
                parameter1,      // Anno
                parameter2,      // NumeroTicket
                response
            );
        }
        else
        {
            TraceLogApp("Error: ticket debe ser numérico: " + line);
            error++;
        }
    }
    else
    {
        TraceLogApp("Error: año debe ser numérico: " + line);
        error++;
    }
}
```

---

## 6. PROCESAMIENTO DE RESPUESTAS DEL SERVICIO WEB

### Objeto acuseRecibo (Respuesta de Envío)

**Fuente:** `ProxyClient.cs` (generado desde WSDL)

**Propiedades:**
```csharp
public class acuseRecibo
{
    public string anhoEnvio;              // Año del envío
    public string documentoEmisor;        // RUC del emisor
    public string fechaRecepcion;         // Fecha/hora recepción
    public string hashDocumento;          // Hash/checksum del documento
    public string numeroOrden;            // Número de orden
    public string ticketEnvio;            // ⭐ IMPORTANTE: número de ticket para consultas posteriores
    public errorAcuse[] listaErrores;     // Array de errores (si hay)
    public errorAcuse[] listaWarning;     // Array de advertencias (si hay)
}

public class errorAcuse
{
    public string codigo;        // Código de error (ej: "91500")
    public string descripcion;   // Descripción legible
    public string tipo;          // Tipo: "error" o "warning"
}
```

**Procesamiento en ThreadProxy:**
```csharp
// ThreadProxy.cs:OnStartSend()
IAsyncResult result = oProxy.BeginrecibirArchivo(
    this.numeroTransaccion, 
    fileArray, 
    null, 
    null
);
result.AsyncWaitHandle.WaitOne();
var valueResult = oProxy.EndrecibirArchivo(result);

Logic.WS.acuseRecibo oTicket = (Logic.WS.acuseRecibo)valueResult;

// Validar errores
if (oTicket.listaErrores != null && oTicket.listaErrores.Length > 0)
{
    foreach (errorAcuse error in oTicket.listaErrores)
    {
        // error.codigo: "91500"
        // error.descripcion: "Usuario o contraseña incorrectos"
    }
}

// Guardar como XML
string fileName = this.directorioSave + "\\" + 
    Logic.Core.GenerateNameAcuse(oTicket.anhoEnvio, oTicket.ticketEnvio);
XmlSerializer x = new XmlSerializer(typeof(Logic.WS.acuseRecibo));
TextWriter writer = new StreamWriter(fileName);
x.Serialize(writer, valueResult);
writer.Close();
```

**Archivo generado (XML):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<acuseRecibo>
  <anhoEnvio>2014</anhoEnvio>
  <documentoEmisor>20100010136</documentoEmisor>
  <fechaRecepcion>2014-06-14 16:07:30</fechaRecepcion>
  <hashDocumento>ABCD1234...</hashDocumento>
  <numeroOrden>001</numeroOrden>
  <ticketEnvio>123456</ticketEnvio>
  <listaErrores>
    <errorAcuse>
      <codigo>91500</codigo>
      <descripcion>Usuario o contraseña incorrectos</descripcion>
      <tipo>error</tipo>
    </errorAcuse>
  </listaErrores>
</acuseRecibo>
```

### Objeto respuestaConsultaBean (Respuesta de Consulta)

**Propiedades:**
```csharp
public class respuestaConsultaBean
{
    public string descripcion;   // Descripción del estado (ej: "ACEPTADO")
    public string tipo;          // Tipo de respuesta: "ACEPTADO", "RECHAZADO", "PROCESANDO"
    // Out parameter: byte[] realizarConsultaResultado (datos binarios de respuesta)
}
```

**Procesamiento en ThreadProxy:**
```csharp
// ThreadProxy.cs:OnStartQuery()
String query = "<consulta><tipo>1</tipo><parametros>" +
    "<numeroTicket>" + this.ticketConsulta.Trim() + "</numeroTicket>" +
    "<annoTicket>" + this.annoConsulta + "</annoTicket>" +
    "</parametros></consulta>";

IAsyncResult result = oProxy.BeginrealizarConsulta(query, null, null);
result.AsyncWaitHandle.WaitOne();

byte[] fileBytes;
Logic.WS.respuestaConsultaBean valueResult = oProxy.EndrealizarConsulta(
    result, 
    out fileBytes  // ⭐ Datos de respuesta en bytes (XML o ZIP)
);

if (fileBytes != null)
{
    string fileName = this.directorioSave + "\\" + 
        Logic.Core.GenerateNameResponse(this.annoConsulta, this.ticketConsulta);
    this.nombreRespuesta = fileName;
    File.WriteAllBytes(@fileName, fileBytes);  // Guardar bytes directamente
}
```

---

## 7. EXTRACCIÓN DE ERRORES DE SOAP FAULTS

### Contexto
Cuando SUNAT devuelve un error SOAP, viene en formato XML embebido en la excepción.

**Ejemplo de SOAP Fault:**
```xml
<S:Envelope xmlns:S="http://schemas.xmlsoap.org/soap/envelope/">
  <S:Header>...</S:Header>
  <S:Body>
    <S:Fault xmlns:ns4="http://www.w3.org/2003/05/soap-envelope">
      <faultcode>91500</faultcode>
      <faultstring>Usuario o contraseña incorrectos.</faultstring>
    </S:Fault>
  </S:Body>
</S:Envelope>
```

### Procesamiento

```csharp
// Core.cs:EvaluateMessageError()
public static string EvaluateMessageError(string message)
{
    const string TAG_XML_1 = "<faultcode>";
    const string TAG_XML_2 = "</faultstring>";
    
    if (message.IndexOf(TAG_XML_1) > 0)
    {
        int length = message.IndexOf(TAG_XML_2) - 
                    message.IndexOf(TAG_XML_1) + 14;
        message = message.Substring(message.IndexOf(TAG_XML_1), length);
        // Extrae: "<faultcode>91500</faultcode><faultstring>..."
    }
    return message;
}
```

**Uso:**
```csharp
// ThreadProxy.cs:OnStartSend()
catch (Exception ex)
{
    string messageEval = Logic.Core.EvaluateMessageError(ex.Message.ToString());
    this.txtMessage.BeginInvoke((Action)(() => { 
        this.txtMessage.AppendText("Error OnStartSend :" + messageEval + Environment.NewLine); 
    }));
    Logic.Core.TraceLogApp("Error OnStartSend : " + messageEval);
}
```

---

## 8. CONFIGURACIÓN PERSISTENTE (App.config)

### Carga de Configuración

```csharp
// FrmMain.cs:LoadValues()
Configuration config = ConfigurationManager.OpenExeConfiguration(
    Application.ExecutablePath
);

this.txtRUC.Text = config.AppSettings.Settings["NumeroRUC"].Value;
this.txtUsuario.Text = config.AppSettings.Settings["UsuarioSOL"].Value;
this.txtCodigoEnvio.Text = config.AppSettings.Settings["CodigoEnvio"].Value;
m_url = config.AppSettings.Settings["UrlServicioWeb"].Value;
```

### Almacenamiento de Configuración

```csharp
// FrmMain.cs:EvtSaveConf()
Configuration config = ConfigurationManager.OpenExeConfiguration(
    Application.ExecutablePath
);
config.AppSettings.Settings.Remove("NumeroRUC");
config.AppSettings.Settings.Add("NumeroRUC", this.txtRUC.Text);
config.AppSettings.Settings.Remove("UsuarioSOL");
config.AppSettings.Settings.Add("UsuarioSOL", this.txtUsuario.Text);
config.AppSettings.Settings.Remove("CodigoEnvio");
config.AppSettings.Settings.Add("CodigoEnvio", this.txtCodigoEnvio.Text);
config.Save(ConfigurationSaveMode.Modified);
```

### Claves Disponibles en App.config

```xml
<appSettings>
  <add key="UrlServicioWeb" value="http://pruebaaduana.sunat.gob.pe/..."/>
  <add key="DirectorioDocumento" value=""/>          <!-- Auto-llena si vacío -->
  <add key="DirectorioAcuse" value=""/>              <!-- Auto-llena si vacío -->
  <add key="DirectorioRespuesta" value=""/>          <!-- Auto-llena si vacío -->
  <add key="UsuarioSOL" value="PILOTONS"/>
  <add key="NumeroRUC" value="20100010136"/>
  <add key="CodigoEnvio" value="540D3D995F35"/>
  
  <!-- Proxy Configuration -->
  <add key="ProxyActivo" value="false"/>
  <add key="ProxyDireccionIP" value=""/>
  <add key="ProxyPuerto" value=""/>
  <add key="ProxyNombreDominio" value=""/>
  <add key="ProxyUsuario" value=""/>
  <add key="ProxyClave" value=""/>
</appSettings>
```

---

## 9. THREADING Y PROCESAMIENTO ASINCRÓNICO

### Modelo de Threading

```
FrmMain.SendFiles() o SendQuerys()
    ↓
Crear N threads (uno por archivo/consulta)
    ↓
Cada thread: new ThreadProxy() → otp.Run()
    ↓
Ejecutar: OnStartSend() o OnStartQuery() en paralelo
    ↓
Recopilar resultados: otp.Join() para sincronizar
    ↓
Actualizar m_dtEnvios / m_dtConsultas
    ↓
UI refresh: grdEnvios.DataSource = m_dtEnvios
```

### Parámetros Compartidos entre Threads

```csharp
// ThreadProxy.cs - Propiedades públicas
otp.Correlativo = (i+1);                           // Número secuencial
otp.Url = m_url;                                   // URL SUNAT
otp.Usuario = Logic.Core.CombineUserData(...);    // Usuario combinado
otp.Clave = this.txtClave.Text;                    // Contraseña
otp.IdProceso = m_dtEnvios.Rows[i][0].ToString(); // ID para sync con DataTable
otp.NumeroTransaccion = ...;                       // Para envíos
otp.TicketConsulta = ...;                          // Para consultas
otp.DirectorioSave = m_pathApp + "\\" + Logic.Core.DIRECTORIO_ACUSE;
otp.TxtMessage = this.txtEventoEnvio;             // TextBox para mensajes (thread-safe)
```

### Actualización Thread-Safe de UI

```csharp
// ThreadProxy.cs - En OnStartSend/OnStartQuery
this.txtMessage.BeginInvoke((Action)(() => { 
    this.txtMessage.AppendText("Mensaje: " + Environment.NewLine); 
}));
```

**BeginInvoke** asegura que la actualización ocurra en el UI thread, no en el worker thread.

---

## 10. DIAGRAMA GENERAL DE FLUJO DE ARCHIVOS

```
┌─────────────────────────────────────────────────────────────┐
│              WINCLIENTESEIDA - FLUJO DE ARCHIVOS              │
└─────────────────────────────────────────────────────────────┘

ENVÍO DE ARCHIVOS ZIP:
========================
Usuario → Selecciona .ZIP / Carga .TXT
    ↓
Archivo agregado a m_dtEnvios (DataTable)
    ↓
[grdEnvios] muestra lista
    ↓
Usuario → Click "Enviar"
    ↓
SendFiles() valida credenciales, IP, MAC
    ↓
Para cada fila en m_dtEnvios:
  • Crear ThreadProxy(type=1, nombre="Task-i")
  • Leer archivo ZIP: File.ReadAllBytes()
  • Llamar SOAP: recibirArchivo(numeroTransaccion, zipBytes)
  • Recibir: acuseRecibo object
  ↓
Guardar acuse como XML en:
  {AppPath}/destino_acuse/seida_acuse_YYYY_TICKET.xml
  ↓
Registrar en BitacoraEventos.txt:
  "Respuesta Hilo(n) | Ticket: XXX | acuse: {ruta}"
  ↓
Limpiar m_dtEnvios.Rows.Clear()
  ↓
[Proceso completado]


CONSULTA DE TICKETS:
===================
Usuario → Ingresa ticket y año / Carga .TXT
    ↓
Entrada agregada a m_dtConsultas (DataTable)
    ↓
[grdConsultas] muestra lista
    ↓
Usuario → Click "Consultar"
    ↓
SendQuerys() valida credenciales, IP, MAC
    ↓
Para cada fila en m_dtConsultas:
  • Crear ThreadProxy(type=2, nombre="Task-i")
  • Armar query XML con ticket y año
  • Llamar SOAP: realizarConsulta(queryXml, out fileBytes)
  • Recibir: respuestaConsultaBean + bytes
  ↓
Si bytes ≠ null:
  Guardar en: {AppPath}/destino_respuesta/seida_respuesta_YYYY_YYYYMMDDHHMMSS_UUID_TICKET.xml
  ↓
Actualizar m_dtConsultas[ArchivoRecibido] con ruta
  ↓
Registrar en BitacoraEventos.txt:
  "Respuesta Hilo(n) | Ticket: XXX | Archivo: {ruta}"
  ↓
[Proceso completado]


LOGGING CONTINUO:
=================
Cualquier operación → Core.TraceLogApp(mensaje)
    ↓
Trace.WriteLine(DateTime.Now.ToLongTimeString() + " " + mensaje)
    ↓
m_listenerLog (TextWriterTraceListener) → m_traceLog (FileStream)
    ↓
{AppPath}/log/BitacoraEventos.txt
    ↓
[Línea agregada: "HH:MM:SS mensaje"]
    ↓
m_listenerLog.Flush() + m_traceLog.Flush() después de cada operación
```

---

## 11. RESUMEN: CICLOS DE LECTURA/ESCRITURA

| Operación | Lectura | Escritura | Actualización |
|-----------|---------|-----------|---------------|
| **App Start** | App.config | Directorios (crear si no existen) | BitacoraEventos.txt (append) |
| **Agregar Envío** | Archivo ZIP local (validar existe) | m_dtEnvios (fila nueva) | grdEnvios DataSource |
| **Agregar Consulta** | - | m_dtConsultas (fila nueva) | grdConsultas DataSource |
| **Enviar (Send)** | ZIP file bytes | SOAP request → SUNAT | acuseRecibo XML en destino_acuse/ |
| **Consultar (Query)** | SOAP request → SUNAT | respuesta bytes → archivo | respuesta XML en destino_respuesta/ |
| **Logging** | Trace listeners | BitacoraEventos.txt (append) | Tiempo real durante operaciones |
| **Config Save** | UI controls | App.config | Configuration.Save() |
| **App Close** | - | Flush+Close listeners | BitacoraEventos.txt finalizacion |

---

## 12. CONSIDERACIONES DE SEGURIDAD EN MANEJO DE ARCHIVOS

1. **Ruta de archivos:** Rutas completas sin sanitización, confía en seleccionador de archivos del SO
2. **Permisos:** Lectura/escritura de archivos requiere permisos del usuario actual
3. **Encriptación de credenciales:** Existe AES encryptor en Core.cs pero **NO se usa para App.config** (credenciales en texto plano)
4. **Proxy credentials:** Se guardan en App.config sin encriptación
5. **SOAP messages:** Incluyen usuario + contraseña + RUC en cada request

