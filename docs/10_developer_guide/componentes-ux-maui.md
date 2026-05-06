# Componentes UX para .NET MAUI

Guía de referencia de los controles MAUI publicados en el paquete
`MotorDsl.Maui`, namespace `MotorDsl.Maui.Controls`. Todos están pensados para
bindearse a una instancia de `IThermalPrinterService` y reflejar su estado
automáticamente vía `INotifyPropertyChanged`.

---

## 1. Visión general

| Componente | Tipo base | Rol |
|---|---|---|
| `PrinterStatusBadge` | `ContentView` | Badge visual con color y texto que refleja el estado de conexión. |
| `PrinterPickerView` | `ContentView` | Botón Escanear + lista de dispositivos descubiertos; conecta al tocar uno. |
| `PrinterPickerPage` | `ContentPage` | Página modal que envuelve `PrinterPickerView` con un Cancelar. |
| `MauiRasterPreview` | `ContentView` | Muestra un PNG (típicamente producido por `RasterPreviewRenderer`) con zoom configurable. |
| `MauiDocumentPreview` | `ContentView` | Vista previa tipográfica de un `LayoutedDocument`. |

Namespace XML:

```xml
xmlns:muic="clr-namespace:MotorDsl.Maui.Controls;assembly=MotorDsl.Maui"
```

---

## 2. `PrinterStatusBadge`

Muestra un badge con color y texto que refleja `ConnectionState`,
`CurrentDevice` y `LastError` de un `IThermalPrinterService`.

### API

| BindableProperty | Tipo | Default | Descripción |
|---|---|---|---|
| `Service` | `IThermalPrinterService?` | `null` | Servicio a observar. Cambiarlo desuscribe del anterior y suscribe al nuevo. |

### Eventos

Ninguno propio. La fuente de verdad es el `INotifyPropertyChanged` del servicio.

### Estados visuales

| `ConnectionState` | Color | Texto |
|---|---|---|
| `Disconnected` | `#9E9E9E` | "Desconectado" |
| `Scanning` | `#1976D2` | "Buscando..." |
| `Connecting` | `#F57C00` | "Conectando..." |
| `Connected` | `#388E3C` | "Conectado: {Name}" |
| `Reconnecting` | `#F57C00` | "Reconectando..." |
| `Failed` | `#D32F2F` | "Error: {LastError}" |

### XAML de uso

```xml
<muic:PrinterStatusBadge x:Name="StatusBadge" />
```

### Code-behind

```csharp
public MainPage(IThermalPrinterService printer)
{
    InitializeComponent();
    StatusBadge.Service = printer;
}
```

### Matriz de bindings

| Origen (servicio) | Disparador | Acción del badge |
|---|---|---|
| `ConnectionState` cambia | `PropertyChanged` | Recalcula color + texto. |
| `CurrentDevice` cambia | `PropertyChanged` | Refresca el nombre cuando está conectado. |
| `LastError` cambia | `PropertyChanged` | Refresca el texto de error. |
| Otra propiedad cualquiera | `PropertyChanged` (con `null`/vacío) | Re-aplica estado por seguridad. |

---

## 3. `PrinterPickerView`

Permite descubrir y conectar dispositivos. Internamente:

1. Botón Escanear → `Service.DiscoverDevicesAsync(FilterKind)`.
2. Si hay 0 → mensaje "Sin dispositivos".
3. Si hay 1 y `AutoConnectIfSingle = true` → conecta automáticamente.
4. Si hay > 1 → lista en `CollectionView`. Al tocar uno, `Service.ConnectAsync(device)`.

### API

| BindableProperty | Tipo | Default | Descripción |
|---|---|---|---|
| `Service` | `IThermalPrinterService?` | `null` | Servicio a usar. |
| `FilterKind` | `string?` | `null` | Si está seteado, filtra el descubrimiento al `Kind` indicado (p.ej. `"bluetooth"`). |
| `AutoConnectIfSingle` | `bool` | `true` | Conectar automáticamente cuando se descubre exactamente un dispositivo. |

### Métodos públicos

| Método | Descripción |
|---|---|
| `Task ScanAsync()` | Dispara un escaneo manualmente. |
| `Task ConnectAsync(PrinterDevice device)` | Conecta a un dispositivo específico (raramente necesario; el list selection lo hace internamente). |

### Eventos

| Evento | Args | Cuándo |
|---|---|---|
| `DeviceSelected` | `PrinterDevice` | El servicio confirmó la conexión. |
| `ScanError` | `Exception` | Falló el escaneo (BT desactivado, permisos denegados, etc.). |

### XAML de uso

```xml
<muic:PrinterPickerView x:Name="DevicePicker"
                        FilterKind="bluetooth"
                        AutoConnectIfSingle="True" />
```

### Code-behind

```csharp
public MainPage(IThermalPrinterService printer)
{
    InitializeComponent();
    DevicePicker.Service = printer;
    DevicePicker.DeviceSelected += (_, dev) =>
        Status.Text = $"Conectado a {dev.Name}";
    DevicePicker.ScanError += (_, ex) =>
        Status.Text = $"BT Error: {ex.Message}";
}

protected override async void OnAppearing()
{
    base.OnAppearing();
    await DevicePicker.ScanAsync();
}
```

### Matriz de bindings

| Property | Aplicado a |
|---|---|
| `Service` | Fuente de `DiscoverDevicesAsync` y `ConnectAsync`. |
| `FilterKind` | Pasado tal cual al `DiscoverDevicesAsync(kind)`. |
| `AutoConnectIfSingle` | Atajo cuando hay un único device descubierto. |

---

## 4. `PrinterPickerPage`

`ContentPage` modal que envuelve `PrinterPickerView` con un botón Cancelar.
Cierra el modal al conectar exitosamente o al cancelar.

### Constructor

```csharp
public PrinterPickerPage(IThermalPrinterService service, string? filterKind = null)
```

### Uso

```csharp
var page = new PrinterPickerPage(_printer, filterKind: "bluetooth");
await Navigation.PushModalAsync(page);
```

---

## 5. `MauiRasterPreview`

Muestra el PNG producido por `RasterPreviewRenderer` con escalado configurable
para simular vista pixelada.

### API

| BindableProperty | Tipo | Default | Descripción |
|---|---|---|---|
| `ImageBytes` | `byte[]?` | `null` | Bytes PNG a mostrar. |
| `ZoomFactor` | `double` | `2.0` | Factor de escalado del tamaño nativo de la imagen. |

### XAML de uso

```xml
<muic:MauiRasterPreview x:Name="RasterPreview" ZoomFactor="2" />
```

### Code-behind

```csharp
private void OnPreviewClicked(object? sender, EventArgs e)
{
    var profile = new DeviceProfile("preview", 32, "raster-preview");
    var result  = _engine.Render(jsonDsl, profile);
    if (result.IsSuccessful && result.Output is byte[] png)
        RasterPreview.ImageBytes = png;
}
```

### Comportamiento

- Si `ImageBytes` es `null` o vacío, muestra un placeholder *"(sin vista previa)"*.
- Decodifica el tamaño nativo con `SkiaSharp` y aplica
  `WidthRequest = nativeWidth * ZoomFactor` para que la imagen no se interpole
  visualmente al escalarse.
- Está envuelta en un `ScrollView` bidireccional para imágenes grandes.

### Matriz de bindings

| Property | Recálculo cuando cambia |
|---|---|
| `ImageBytes` | Decodifica tamaño nativo y reasigna `Source`. |
| `ZoomFactor` | Recalcula `WidthRequest` / `HeightRequest`. |

---

## 6. `MauiDocumentPreview`

Vista previa tipográfica de un `LayoutedDocument`. Renderiza cada
`LayoutInfo.WrappedText` como `Label` aplicando alineación, bold, e indicadores
visuales para nodos QR / barcode / bitmap.

### API

| BindableProperty | Tipo | Default | Descripción |
|---|---|---|---|
| `Document` | `LayoutedDocument?` | `null` | Documento ya pasado por el `LayoutEngine`. |

### XAML de uso

```xml
<muic:MauiDocumentPreview x:Name="DocPreview" />
```

### Code-behind

```csharp
var profile = new DeviceProfile("preview", 32, "text");
var result = _engine.Render(jsonDsl, profile);
if (result is RenderResult { LayoutedDocument: { } layouted })
    DocPreview.Document = layouted;
```

> Nota: `MauiDocumentPreview` no produce el `LayoutedDocument` por sí solo —
> espera recibirlo. Para una vista pixelada listo-para-imprimir conviene usar
> `MauiRasterPreview` con el target `raster-preview`.

### Matriz de bindings (estilos derivados de `LayoutInfo.DeviceMetadata`)

| Metadata key | Estilo aplicado |
|---|---|
| `bold = true` | `FontAttributes = Bold`. |
| `is_qr` o `is_barcode` | Texto en `DarkBlue`, `Italic`. |
| `is_bitmap` | Texto en `DarkGreen`, `Italic`. |
| `align = center` | `HorizontalOptions = Center`. |
| `align = right` | `HorizontalOptions = End`. |

---

## 7. Patrones recomendados

### 7.1 Bind único en `OnAppearing`

```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    StatusBadge.Service  = _printer;
    DevicePicker.Service = _printer;
}
```

Evitar bindear en cada navegación: el servicio es singleton y suscribirse
varias veces causaría doble update por cambio de propiedad.

### 7.2 Manejo de errores del picker

```csharp
DevicePicker.ScanError += async (_, ex) =>
{
    if (ex.Message.Contains("BLUETOOTH_CONNECT"))
        await DisplayAlert("Permisos", "Aceptá BT y reescanea", "OK");
    else
        await DisplayAlert("BT", ex.Message, "OK");
};
```

### 7.3 Compatibilidad iOS

Los controles BT no deben mostrarse en iOS:

```xml
<muic:PrinterStatusBadge IsVisible="{OnPlatform Android=True, iOS=False}" />
<muic:PrinterPickerView  IsVisible="{OnPlatform Android=True, iOS=False}" />
```

`MauiRasterPreview` y `MauiDocumentPreview` funcionan igual en ambas
plataformas.

---

## 8. Limitaciones conocidas

- `MauiRasterPreview` decodifica el PNG con `SkiaSharp`. Si la imagen es
  grande (> 2 MB) la decodificación bloquea el hilo.
- `PrinterPickerView` no soporta selección múltiple ni reordenamiento.
- El `ScrollView` interno de `MauiRasterPreview` no permite gestos de pinch
  para zoom; el zoom se controla por `ZoomFactor`.
- Cambios de `Service` después de mostrar el control sí están soportados
  (desuscribe del anterior y se suscribe al nuevo), pero conviene fijarlo una
  sola vez en `OnAppearing`.

---

## 9. Referencias

- [Guía de Integración MAUI](guia-integracion-maui.md)
- [Render Pixelado y PDF](render-pixelado-y-pdf.md)
- [Transports y Extensibilidad](transports-y-extensibilidad.md)
- [Arquitectura de la Solución (v1.1)](../05_arquitectura_tecnica/arquitectura-solucion_v1.1.md)
