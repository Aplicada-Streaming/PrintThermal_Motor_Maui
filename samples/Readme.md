# Samples MotorDsl

Aplicaciones .NET MAUI de demostración. Cada una ilustra un caso de uso
distinto y un modo de consumo diferente del Motor.

---

## 📋 Catálogo

| Sample | Descripción | Modo de consumo |
|---|---|---|
| MotorDsl.SampleApp | Demo mínimo: render a texto y ESC/POS sin transport | ProjectReference |
| MotorDsl.MultaApp | Sample completo de multa de tránsito con servicios MAUI locales | ProjectReference |
| MotorDsl.Integrated.MultaApp | Sample con DSL en formato `integrated` (datos pre-resueltos) | ProjectReference |
| MotorDsl.Nuget.MultaApp | Equivalente a MultaApp pero consumiendo los paquetes NuGet | PackageReference + ProjectReference (Fase 1) |
| MotorDsl.Nuget.Integrated.MultaApp | Equivalente a Integrated pero vía NuGet | PackageReference + ProjectReference (Fase 1) |

> **Fase 1**: hasta que `MotorDsl.Maui`, `MotorDsl.Bluetooth` y
> `MotorDsl.Printing.Abstractions` estén publicados en nuget.org, los samples
> NuGet los consumen vía `<ProjectReference>` y consumen los 4 paquetes
> originales (`Core`, `Parser`, `Rendering`, `Extensions`) vía
> `<PackageReference>`.

---

## 🧪 MotorDsl.Nuget.Integrated.MultaApp (recomendado como punto de partida)

App MAUI completa que combina **formato integrado** del DSL + consumo desde
NuGet + controles MAUI. Es el sample más cercano al patrón canónico que
recomendamos a un desarrollador externo.

### MauiProgram.cs (relevante)

```csharp
using Microsoft.Extensions.Logging;
using MotorDsl.Bluetooth;
using MotorDsl.Core.Models;
using MotorDsl.Extensions;
using MotorDsl.Maui;
using MotorDsl.Nuget.Integrated.MultaApp.Pages;
using MotorDsl.Nuget.Integrated.MultaApp.Templates;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

    builder.Services.AddMotorDslEngine()
        .AddTemplates(t =>
        {
            t.Add("acta-infraccion-integrada", MultaIntegratedDsl.Document);
        })
        .AddProfiles(p =>
        {
            p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
            p.Add(new DeviceProfile("a4-pdf", 80, "pdf"));
            p.Add(new DeviceProfile("pdf",    48, "pdf"));
        })
        .AddMotorDslMaui();

    builder.Services.AddBluetoothPrinterTransport();
    builder.Services.AddTransient<MainPage>();
    return builder.Build();
}
```

### MainPage.xaml (relevante)

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:muic="clr-namespace:MotorDsl.Maui.Controls;assembly=MotorDsl.Maui"
             x:Class="MotorDsl.Nuget.Integrated.MultaApp.Pages.MainPage">
    <VerticalStackLayout Padding="12" Spacing="10">
        <muic:PrinterStatusBadge x:Name="StatusBadge"
                                 IsVisible="{OnPlatform Android=True, iOS=False}" />
        <muic:PrinterPickerView  x:Name="DevicePicker" FilterKind="bluetooth"
                                 IsVisible="{OnPlatform Android=True, iOS=False}" />
        <Picker x:Name="DocPicker" SelectedIndexChanged="OnDocPickerChanged" />
        <Button x:Name="BtnPreview"  Text="Vista Previa" Clicked="OnVistaPreviewClicked" />
        <Button x:Name="BtnImprimir" Text="Imprimir"     Clicked="OnImprimirClicked"
                IsVisible="{OnPlatform Android=True, iOS=False}" />
        <Button x:Name="BtnVerPdf"   Text="Ver PDF"      Clicked="OnVerPdfClicked" />
    </VerticalStackLayout>
</ContentPage>
```

### MainPage.xaml.cs (relevante)

```csharp
public partial class MainPage : ContentPage
{
    private readonly IDocumentEngine _engine;
    private readonly IThermalPrinterService _printer;

    public MainPage(IDocumentEngine engine, IThermalPrinterService printer)
    {
        InitializeComponent();
        _engine  = engine;
        _printer = printer;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StatusBadge.Service  = _printer;
        DevicePicker.Service = _printer;
#if ANDROID
        if (await RequestBluetoothPermissions())
            await DevicePicker.ScanAsync();
#endif
    }

    private async void OnImprimirClicked(object? sender, EventArgs e)
    {
        var profile = new DeviceProfile("58HB6", 32, "escpos-bitmap");
        profile.SetCapability("supports_bitmap", true);
        var result = _engine.Render(MultaIntegratedDsl.Document, profile);
        if (result.IsSuccessful && _printer.IsConnected && result.Output is byte[] bytes)
            await _printer.SendBytesAsync(bytes);
    }
}
```

---

## 🧪 MotorDsl.Nuget.MultaApp

Mismo patrón que el Integrated, pero consumiendo el formato **template + data**
del DSL. Diferencia clave: los templates usan `{{placeholders}}`, `loop` y
`conditional`, y la app llama a `engine.Render(json, data, profile)`.

### MauiProgram.cs (relevante)

```csharp
builder.Services.AddMotorDslEngine()
    .AddTemplates(t =>
    {
        t.Add("acta-infraccion",  MultaDsl.Template);
        t.Add("ticket-simple",    TicketSimpleDsl.Template);
        t.Add("comprobante-pago", ComprobanteDsl.Template);
    })
    .AddProfiles(p =>
    {
        p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
        p.Add(new DeviceProfile("pdf", 48, "pdf"));
    })
    .AddMotorDslMaui();

builder.Services.AddBluetoothPrinterTransport();
```

### Diferencia con Nuget.Integrated.MultaApp

- Templates con `{{}}` y `loop` en lugar de JSON ya resuelto.
- Llamada al motor: `_engine.Render(template, data, profile)` (con `data`).
- Pipeline interno: incluye etapa **Evaluator**.

---

## 🧪 MotorDsl.Integrated.MultaApp (ProjectReference)

Equivalente al `Nuget.Integrated.MultaApp` pero usando `<ProjectReference>` a
`src/MotorDsl.*` en lugar de `<PackageReference>`. Pensado para iterar sobre
la librería sin tener que publicar paquetes.

`MauiProgram.cs` registra los renderers manualmente porque consume directamente
el código de `MotorDsl.Maui`:

```csharp
builder.Services.AddMotorDslEngine()
    .AddTemplates(t => { t.Add("acta-infraccion-integrada", MultaIntegratedDsl.Document); })
    .AddProfiles(p =>
    {
        p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
        p.Add(new DeviceProfile("pdf", 48, "pdf"));
    })
    .AddRenderer<PdfRenderer>()
    .AddRenderer<BitmapEscPosRenderer>();

builder.Services.AddSingleton<IBitmapRasterizer, SkiaSharpRasterizer>();
builder.Services.AddSingleton<IPrintErrorHandler, DefaultPrintErrorHandler>();
builder.Services.AddSingleton<IThermalPrinterService, ThermalPrinterService>();
```

> Nota: este sample existe **antes** del refactor que extrajo los renderers a
> `MotorDsl.Maui`. Es válido como referencia histórica, pero la forma
> recomendada para nuevos proyectos es `Nuget.Integrated.MultaApp`.

---

## 🧪 MotorDsl.MultaApp (ProjectReference, template + data)

Sample histórico. Consume `MotorDsl.*` vía `<ProjectReference>` y trae los
renderers + servicios de impresión + controles MAUI **locales** en su propio
código (carpetas `Renderers/`, `Services/`, `Controls/`). Útil para ver cómo
era el patrón antes del refactor.

```csharp
builder.Services.AddMotorDslEngine()
    .AddTemplates(t =>
    {
        t.Add("acta-infraccion",  MultaDsl.Template);
        t.Add("ticket-simple",    TicketSimpleDsl.Template);
        t.Add("comprobante-pago", ComprobanteDsl.Template);
    })
    .AddProfiles(p =>
    {
        p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
        p.Add(new DeviceProfile("pdf", 48, "pdf"));
    })
    .AddRenderer<PdfRenderer>()
    .AddRenderer<BitmapEscPosRenderer>();
```

---

## 🧪 MotorDsl.SampleApp

Sample mínimo: render a texto y ESC/POS clásico de un ticket de venta simple.
No tiene PDF ni renderer de bitmap. Útil para entender el pipeline DSL puro.

```csharp
builder.Services.AddMotorDslEngine()
    .AddTemplates(t => { t.Add("ticket-venta", TicketDsl.Template); })
    .AddProfiles(p => { p.Add(new DeviceProfile("thermal_58mm", 32, "escpos")); });

builder.Services.AddSingleton<IPrintErrorHandler, DefaultPrintErrorHandler>();
builder.Services.AddSingleton<IThermalPrinterService, ThermalPrinterService>();
```

---

## 🚀 Cómo correr

```bash
dotnet build -t:Run -f net10.0-android samples/<NombreSample>/<NombreSample>.csproj
```

Atajos `.bat`:

- `scripts/local/run-MotorDsl.<Nombre>.bat` — corre el sample en un dispositivo
  conectado por ADB.
- `scripts/local/run-All.bat` — corre todos en orden.
- `scripts/mobile/publish-MotorDsl.<Nombre>-apk.bat` — empaqueta APK firmado.

Más detalles en
[`scripts/local/Readme.md`](../scripts/local/Readme.md) y
[`scripts/mobile/Readme.md`](../scripts/mobile/Readme.md).

---

## 🐛 Debugging Android

Logs adb, comandos para inspeccionar packages instalados, troubleshooting
típico (QuestPDF / Skia / `libstdc++` / 16k pages) en
[`samples/notas-debug-android.md`](notas-debug-android.md).
