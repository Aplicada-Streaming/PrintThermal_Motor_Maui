# Ejemplo 03 — MotorDsl.Nuget.MultaApp

> Réplica de `MotorDsl.MultaApp` que integra el motor a través de los paquetes
> NuGet publicados en nuget.org, en lugar de referencias de proyecto locales.
> Sirve de test de integración end-to-end y como ejemplo canónico para el
> usuario final.

**Estado:** Implementado — refactor v1.1 (3 paquetes nuevos: `MotorDsl.Maui`, `MotorDsl.Bluetooth`, `MotorDsl.Printing.Abstractions`)
**Ubicación:** `samples/MotorDsl.Nuget.MultaApp/`

---

## 1. Propósito y Audiencia

Esta aplicación tiene dos objetivos complementarios:

1. **Test de integración NuGet:** valida que los 7 paquetes funcionen
   correctamente en una app MAUI real, con los mismos templates que
   `MotorDsl.MultaApp` y consumiendo controles + renderers desde
   `MotorDsl.Maui`.
2. **Ejemplo para el usuario final:** demuestra la forma canónica de integrar
   MotorDsl en un proyecto nuevo, como si fuera un desarrollador externo que
   instala la librería desde NuGet.

**Nivel:** Avanzado
**Audiencia:** Desarrolladores que desean integrar MotorDsl en sus propios
proyectos MAUI.

---

## 2. Diferencia clave con MotorDsl.MultaApp

| Aspecto | MotorDsl.MultaApp | MotorDsl.Nuget.MultaApp |
|---|---|---|
| Referencias al motor | `<ProjectReference>` locales | `<PackageReference>` desde nuget.org |
| ApplicationId | `com.motordsl.multaapp` | `com.motordsl.nuget.multaapp` |
| Namespace | `MotorDsl.MultaApp.*` | `MotorDsl.Nuget.MultaApp.*` |
| Renderers MAUI | Locales en `Renderers/` | Llegan vía `MotorDsl.Maui` |
| Servicio de impresión | Local en `Services/` | Llega vía `MotorDsl.Printing.Abstractions` |
| Controles MAUI | Locales en `Controls/` | Llegan vía `MotorDsl.Maui` |
| Propósito principal | Desarrollo del motor | Consumidor final / integración |

---

## 3. Paquetes NuGet consumidos

```xml
<!-- Stack core -->
<PackageReference Include="MotorDsl.Core"       Version="<latest>" />
<PackageReference Include="MotorDsl.Parser"     Version="<latest>" />
<PackageReference Include="MotorDsl.Rendering"  Version="<latest>" />
<PackageReference Include="MotorDsl.Extensions" Version="<latest>" />

<!-- Stack de impresión + UI MAUI (3 paquetes nuevos) -->
<PackageReference Include="MotorDsl.Printing.Abstractions" Version="<latest>" />
<PackageReference Include="MotorDsl.Bluetooth"             Version="<latest>" />
<PackageReference Include="MotorDsl.Maui"                  Version="<latest>" />
```

> Nota: alcanza con declarar `MotorDsl.Maui` y `MotorDsl.Bluetooth`. El resto
> llega como dependencias transitivas. La lista completa figura sólo a fines
> de claridad.

> Hasta que las primeras versiones de `MotorDsl.Maui`, `MotorDsl.Bluetooth` y
> `MotorDsl.Printing.Abstractions` salgan publicadas en nuget.org, el sample
> consume esos 3 vía `<ProjectReference>` (Fase 1) y los 4 originales vía
> `<PackageReference>`.

---

## 4. Estructura de archivos

```text
samples/MotorDsl.Nuget.MultaApp/
├── MotorDsl.Nuget.MultaApp.csproj   ← PackageReference (4) + ProjectReference (3, fase 1)
├── App.xaml / App.xaml.cs
├── AppShell.xaml / AppShell.xaml.cs
├── MauiProgram.cs                   ← AddMotorDslEngine + AddMotorDslMaui + AddBluetoothPrinterTransport
├── Pages/
│   ├── MainPage.xaml                ← usa muic:PrinterStatusBadge, muic:PrinterPickerView
│   └── MainPage.xaml.cs
├── Platforms/
│   ├── Android/ (AndroidManifest.xml + MainActivity + MainApplication)
│   └── iOS/     (AppDelegate + Program + Info.plist)
├── Resources/   (AppIcon, Fonts, Splash, Styles, Raw)
└── Templates/
    ├── MultaDsl.cs
    ├── TicketSimpleDsl.cs
    └── ComprobanteDsl.cs
```

> No tiene carpetas `Renderers/`, `Services/` ni `Controls/`. Esos artefactos
> ahora viven en `MotorDsl.Maui` y se consumen como paquete.

---

## 5. Funcionalidades

| Feature | Descripción |
|---|---|
| Vista previa | `RasterPreviewRenderer` + `MauiRasterPreview` (PNG con zoom). |
| PDF | `PdfRenderer` (target `pdf`) + `Launcher.OpenAsync`. |
| Impresión ESC/POS bitmap | `BitmapEscPosRenderer` (target `escpos-bitmap`) + `IThermalPrinterService.SendBytesAsync`. |
| Selección de impresora | `muic:PrinterPickerView` bindeado al servicio. |
| Estado de conexión | `muic:PrinterStatusBadge` bindeado al servicio. |
| Bluetooth (Android) | `MotorDsl.Bluetooth.BluetoothPrinterTransport` registrado en DI. |
| iOS | DSL + render + PDF + raster funcionan; controles BT ocultos por `OnPlatform`. |
| Templates | Acta de infracción, Ticket simple, Comprobante de pago. |

---

## 6. Configuración DI (`MauiProgram.cs`)

```csharp
using Microsoft.Extensions.Logging;
using MotorDsl.Bluetooth;
using MotorDsl.Core.Models;
using MotorDsl.Extensions;
using MotorDsl.Maui;
using MotorDsl.Nuget.MultaApp.Pages;
using MotorDsl.Nuget.MultaApp.Templates;
using MotorDsl.Printing;

namespace MotorDsl.Nuget.MultaApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddMotorDslEngine()
            .AddTemplates(t =>
            {
                t.Add("acta-infraccion", MultaDsl.Template);
                t.Add("ticket-simple",   TicketSimpleDsl.Template);
                t.Add("comprobante-pago", ComprobanteDsl.Template);
            })
            .AddProfiles(p =>
            {
                p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
                p.Add(new DeviceProfile("a4-pdf", 80, "pdf"));
                p.Add(new DeviceProfile("pdf",    48, "pdf"));
            })
            .AddMotorDslMaui();              // PDF + ESC/POS bitmap + raster + IThermalPrinterService

        builder.Services.AddBluetoothPrinterTransport();   // Transport Android

        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
```

---

## 7. `MainPage.xaml.cs` (relevante)

```csharp
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using MotorDsl.Printing;

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
        DevicePicker.DeviceSelected += (_, dev) => ShowMessage($"Conectado a {dev.Name}");

#if ANDROID
        if (await RequestBluetoothPermissions())
            await DevicePicker.ScanAsync();
#endif
    }

    private async void OnImprimirClicked(object? sender, EventArgs e)
    {
        var profile = new DeviceProfile("58HB6", 32, "escpos-bitmap");
        profile.SetCapability("supports_bitmap", true);
        profile.SetCapability("bitmap_max_width_px", 320);

        var result = _engine.Render(MultaDsl.Template, profile);
        if (!result.IsSuccessful || result.Output is not byte[] bytes) return;

        if (!_printer.IsConnected) { ShowMessage("Sin impresora"); return; }
        await _printer.SendBytesAsync(bytes);
    }
}
```

---

## 8. `MainPage.xaml` (relevante)

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:muic="clr-namespace:MotorDsl.Maui.Controls;assembly=MotorDsl.Maui"
             x:Class="MotorDsl.Nuget.MultaApp.Pages.MainPage">
    <VerticalStackLayout Padding="12" Spacing="10">
        <muic:PrinterStatusBadge x:Name="StatusBadge"
                                 IsVisible="{OnPlatform Android=True, iOS=False}" />
        <muic:PrinterPickerView x:Name="DevicePicker" FilterKind="bluetooth"
                                IsVisible="{OnPlatform Android=True, iOS=False}" />
        <Button x:Name="BtnImprimir" Text="Imprimir" Clicked="OnImprimirClicked"
                IsVisible="{OnPlatform Android=True, iOS=False}" />
    </VerticalStackLayout>
</ContentPage>
```

---

## 9. Cómo ejecutar

```bash
# Android
dotnet build -t:Run -f net10.0-android samples/MotorDsl.Nuget.MultaApp/MotorDsl.Nuget.MultaApp.csproj

# iOS (macOS con workload ios instalado)
dotnet build -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 samples/MotorDsl.Nuget.MultaApp/MotorDsl.Nuget.MultaApp.csproj
```

Atajos en `scripts/local/run-MotorDsl.Nuget.MultaApp.bat` y
`scripts/mobile/publish-MotorDsl.Nuget.MultaApp-apk.bat`.

---

## 10. Cómo usar como punto de partida para un proyecto propio

1. Copiar la carpeta `samples/MotorDsl.Nuget.MultaApp/`.
2. Renombrar el `.csproj` y los namespaces al nombre de tu proyecto.
3. Personalizar los templates DSL en `Templates/`.
4. Ajustar permisos en `Platforms/Android/AndroidManifest.xml`.
5. Compilar con `dotnet build -f net10.0-android`.

No se requiere clonar el repositorio del Motor — todo llega desde NuGet.

---

## 11. Control de cambios

| Versión | Fecha      | Autor     | Descripción                                              |
|---------|------------|-----------|----------------------------------------------------------|
| v1.0    | 2026-04-02 | DevOps    | Creación del ejemplo como consumidor de NuGet v1.0.2.    |
| v1.1    | 2026-05-06 | Equipo    | Migrado a `MotorDsl.Maui` + `MotorDsl.Bluetooth` + nueva API `IThermalPrinterService`. Eliminadas carpetas locales `Services/`, `Renderers/`, `Controls/`. |
