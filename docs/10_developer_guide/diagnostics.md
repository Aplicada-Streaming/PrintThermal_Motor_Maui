# Diagnóstico y reporte de fallos

`MotorDsl.Maui` incluye una capacidad de **captura y reporte de información de
diagnóstico** orientada a soporte y QA en campo. La feature permite materializar
un snapshot de la aplicación, el dispositivo, las librerías cargadas y la
impresora vinculada, y exponerlo en tres formatos:

1. **Pantalla** (preview pixelado o vista detallada de los datos),
2. **Ticket térmico** (impreso con el mismo motor DSL),
3. **Compartir** vía Share API (email, WhatsApp, clipboard) para reporte de
   fallos.

La captura es **manual y opt-in**: el usuario inicia el proceso desde un botón.
No hay auto-captura ante errores — la feature está pensada como herramienta de
soporte controlada por el operador, no como telemetría implícita.

---

## 1. Datos capturados

| Categoría | Datos |
|---|---|
| **Librería** | Para cada assembly cargado cuyo nombre empiece con `MotorDsl.`: nombre, versión semántica (`AssemblyVersion`) y versión informacional (`AssemblyInformationalVersion`, que suele incluir el sufijo de commit/build). |
| **Aplicación** | Nombre comercial (`AppInfo.Name`), versión semántica (`VersionString`), build number (`BuildString`) y package name del paquete instalado. |
| **Dispositivo** | Fabricante, modelo, plataforma OS, versión OS, idiom (Phone/Tablet/Desktop) y tipo (Physical/Virtual). |
| **Impresora** | Solo si hay un `IThermalPrinterService` con `CurrentDevice != null`: kind del transport, identificador (MAC ofuscada por default), nombre, estado de conexión, profile y ancho del papel. |
| **Permisos** | Solo Android: estado actual de los permisos `Bluetooth` y `LocationWhenInUse` (Granted/Denied/Unknown/etc). |
| **Notas** | Texto libre que el usuario adjunta al iniciar la captura (motivo del reporte). |
| **Fecha** | `DateTimeOffset.Now` con offset de zona local. |

---

## 2. API: `IDiagnosticsReportProvider`

El contrato vive en `MotorDsl.Printing.Abstractions` (paquete neutro), y la
implementación MAUI vive en `MotorDsl.Maui`.

```csharp
namespace MotorDsl.Printing;

public interface IDiagnosticsReportProvider
{
    /// <summary>Construye el snapshot actual de diagnóstico.</summary>
    /// <param name="notes">Texto libre que el usuario adjunta (opcional).</param>
    /// <param name="includePii">Si true, incluye datos como MAC completa. Default false.</param>
    DiagnosticsReport Build(string? notes = null, bool includePii = false);

    /// <summary>Serializa a JSON estructurado (para email/share/log).</summary>
    string ToJson(DiagnosticsReport report);

    /// <summary>Serializa a texto plano formateado (para clipboard).</summary>
    string ToPlainText(DiagnosticsReport report);

    /// <summary>
    /// Genera un DSL "integrated" (formato JSON del motor) listo para ser
    /// renderizado por IDocumentEngine.Render con cualquier profile.
    /// </summary>
    /// <param name="paperWidthChars">Ancho del papel en chars (32 para 58mm, 48 para 80mm).</param>
    string ToDslJson(DiagnosticsReport report, int paperWidthChars = 32);
}
```

El registro DI se realiza automáticamente al llamar `AddMotorDslMaui()` mediante
`TryAddSingleton`, lo que permite al consumer registrar su propio provider antes
y que prevalezca:

```csharp
builder.Services.AddMotorDslEngine()
    .AddMotorDslMaui();   // Registra MauiDiagnosticsReportProvider si nadie lo hizo antes.
```

---

## 3. Patrón de uso típico

El patrón canónico es exponer **tres botones** en la UI: ver, imprimir y
reportar.

```xml
<Grid ColumnDefinitions="*,*,*" ColumnSpacing="8">
    <Button x:Name="BtnDiagVer"        Text="Ver Diag."      Grid.Column="0"
            Clicked="OnDiagnosticoVerClicked" />
    <Button x:Name="BtnDiagImprimir"   Text="Imprimir Diag." Grid.Column="1"
            Clicked="OnDiagnosticoImprimirClicked"
            IsVisible="{OnPlatform Android=True, iOS=False}" />
    <Button x:Name="BtnDiagReportar"   Text="Reportar Fallo" Grid.Column="2"
            Clicked="OnDiagnosticoReportarClicked" />
</Grid>
```

```csharp
public MainPage(IDocumentEngine engine,
                IThermalPrinterService printer,
                IDiagnosticsReportProvider diagnostics)
{
    InitializeComponent();
    _engine = engine;
    _printer = printer;
    _diagnostics = diagnostics;
}

// Ver: render raster preview
private void OnDiagnosticoVerClicked(object? s, EventArgs e)
{
    var report  = _diagnostics.Build(notes: "captura manual");
    var dsl     = _diagnostics.ToDslJson(report, paperWidthChars: 32);
    var profile = new DeviceProfile("preview", 32, "raster-preview");
    profile.SetCapability("bitmap_max_width_px", 384);
    var result  = _engine.Render(dsl, profile);
    if (result.IsSuccessful && result.Output is byte[] bytes)
        RasterPreview.ImageBytes = bytes;
}

// Imprimir: render ESC/POS y enviar al transport
private async void OnDiagnosticoImprimirClicked(object? s, EventArgs e)
{
    var report  = _diagnostics.Build(notes: "imprimir diag");
    var dsl     = _diagnostics.ToDslJson(report);
    var profile = new DeviceProfile("58HB6", 32, "escpos-bitmap");
    profile.SetCapability("supports_bitmap", true);
    profile.SetCapability("bitmap_max_width_px", 320);
    var result  = _engine.Render(dsl, profile);
    if (result.IsSuccessful && _printer.IsConnected)
        await _printer.SendBytesAsync((byte[])result.Output!);
}

// Reportar: Share API (email / WhatsApp / clipboard)
private async void OnDiagnosticoReportarClicked(object? s, EventArgs e)
{
    var report = _diagnostics.Build(notes: "Reporte de fallo");
    var text   = _diagnostics.ToPlainText(report);
    await Share.Default.RequestAsync(new ShareTextRequest
    {
        Title = "Reporte de fallo MotorDsl",
        Text  = text
    });
}
```

El reporte impreso incluye un **código QR con un payload compacto** del estilo
`ver=<motormaui>|app=<appver>|dev=<modelo>|os=<plataforma>_<version>|t=<unix>`.
Ese QR permite correlacionar un ticket físico de soporte con un registro
electrónico sin tener que transcribir versiones a mano.

---

## 4. Privacidad: el flag `includePii` y `MaskMac`

Por default, el `Id` de la impresora (que en Bluetooth Classic es la **MAC
address**) se ofusca antes de incluirlo en el reporte:

| Entrada | Salida con `includePii=false` (default) | Salida con `includePii=true` |
|---|---|---|
| `DC:0D:30:12:34:56` | `DC:0D:30:**:**:**` | `DC:0D:30:12:34:56` |
| `bt-12345678` | `bt-1...` | `bt-12345678` |

La justificación: las MAC addresses son cuasi-identificadores de hardware que
en algunos países se consideran datos personales (GDPR art. 4.1, AEPD).
Para reportes que el usuario va a compartir por email o WhatsApp, ofuscar es
la opción segura. El consumer puede pedir explícitamente la MAC completa
pasando `includePii: true` cuando el reporte se va a un canal interno de
soporte controlado.

---

## 5. Customización

### 5.1 Reemplazar el provider

Si el consumer quiere agregar campos propios (cliente, sucursal, número de
serie del activo, etc), puede registrar **antes** de `AddMotorDslMaui()` su
propia implementación:

```csharp
builder.Services.AddSingleton<IDiagnosticsReportProvider, MyEnterpriseProvider>();
builder.Services.AddMotorDslEngine().AddMotorDslMaui();
```

`AddMotorDslMaui` usa `TryAddSingleton` para no pisar registros previos.

### 5.2 Decorar el provider default

Para extender en lugar de reemplazar, se puede usar el patrón decorator:

```csharp
public class EnterpriseProviderDecorator(IDiagnosticsReportProvider inner) : IDiagnosticsReportProvider
{
    public DiagnosticsReport Build(string? notes = null, bool includePii = false)
    {
        var baseReport = inner.Build(notes, includePii);
        return baseReport with
        {
            Notes = $"Sucursal: {GetBranchCode()} — {baseReport.Notes}"
        };
    }

    public string ToJson(DiagnosticsReport r)        => inner.ToJson(r);
    public string ToPlainText(DiagnosticsReport r)   => inner.ToPlainText(r);
    public string ToDslJson(DiagnosticsReport r, int w = 32) => inner.ToDslJson(r, w);
}
```

### 5.3 Vista detallada en pantalla

`MotorDsl.Maui` también expone `MauiDiagnosticsView` (`ContentView`) para
mostrar el reporte como tarjetas seccionadas en la UI. Es opt-in y no se
inyecta automáticamente en los samples — está disponible para cuando el
consumer quiera ofrecer una pantalla "ver detalle" antes de imprimir o
compartir.

```xml
<muic:MauiDiagnosticsView Report="{Binding LastReport}" />
```

---

## 6. Limitaciones conocidas

- **`PrinterInfoSnapshot.ProfileName` y `Capabilities` no se capturan** en la
  versión actual: el `IThermalPrinterService` no expone el último profile
  utilizado en su contrato público. Esos campos quedan en `(unknown)` /
  diccionario vacío hasta una iteración futura del orquestador.
- **`PermissionsSnapshot` solo se captura en Android.** En iOS y otras
  plataformas la propiedad queda en `null` (los permisos relevantes para
  Bluetooth Classic SPP son Android-only).
- **El payload del QR es texto plano**, no encriptado. No usar para datos
  sensibles. El QR es para correlación, no para autenticación.

---

## 7. Referencias cruzadas

- [Guía de integración MAUI](guia-integracion-maui.md) — sección "Diagnóstico y reporte de fallos"
- [Componentes UX MAUI](componentes-ux-maui.md)
- [Render Pixelado y PDF](render-pixelado-y-pdf.md)
