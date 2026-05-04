# Ejemplos de Uso — MotorDsl

> Aplicaciones de ejemplo que demuestran cómo integrar la librería MotorDsl en proyectos .NET MAUI.

---

## Índice de Ejemplos

| #  | Proyecto                          | Nivel     | Descripción                                                                  |
|----|-----------------------------------|-----------|------------------------------------------------------------------------------|
| 01 | MotorDsl.SampleApp                | Básico    | Ticket simple — aprender la librería paso a paso                             |
| 02 | MotorDsl.MultaApp                 | Avanzado  | Multa de tránsito (template + datos) — todas las funcionalidades clásicas   |
| 03 | MotorDsl.Integrated.MultaApp      | Avanzado  | Multa de tránsito (formato integrado) — JSON pre-resuelto, sin Evaluate     |
| 04 | MotorDsl.MultaApp.Nuget           | Avanzado  | Idem MultaApp — consume los paquetes NuGet publicados en nuget.org           |

---

## Ejemplo 01 — MotorDsl.SampleApp

**Ubicación:** `samples/MotorDsl.SampleApp/`

### Qué demuestra

- Configuración inicial con `AddMotorDslEngine()`
- Template DSL básico con bindings (`{{campo}}`)
- Renderizado a texto plano y ESC/POS
- Conexión Bluetooth con impresora térmica
- Envío de comandos ESC/POS al dispositivo
- Manejo de errores de impresión con reintentos

### Requisitos previos

- .NET 10 SDK
- Visual Studio 2022+ con workload MAUI
- Dispositivo Android físico (para Bluetooth)
- Impresora térmica 58mm compatible ESC/POS (opcional — funciona sin impresora para preview)

### UI

Pantalla única con:
- Botón **Escanear** para descubrir impresoras Bluetooth
- Lista de dispositivos encontrados
- Botón **Imprimir** que renderiza el template y envía a la impresora seleccionada
- Área de texto con el hex dump del resultado ESC/POS

---

## Ejemplo 02 — MotorDsl.MultaApp

**Ubicación:** `samples/MotorDsl.MultaApp/` *(Sprint 08)*

### Qué demuestra

- Template DSL complejo con múltiples tipos de nodo (text, container, loop, conditional, image, table)
- Logo como imagen base64 embebida en el template
- Loop de infracciones con tabla
- Código QR de pago
- Firma digital del agente
- Validación formal de template (`ITemplateValidator`)
- Validación de datos (`IDataValidator`) con warnings para campos null
- Validación de profile (`IProfileValidator`)
- Preview en MAUI (vista previa en pantalla)
- Renderizado ESC/POS para impresora térmica
- Renderizado PDF con QuestPDF (implementado por la app, no por la librería)
- Hex dump para debug
- Exportar ticket a API REST vía `ToBase64()`

### Requisitos previos

- .NET 10 SDK
- Visual Studio 2022+ con workload MAUI
- Dispositivo Android físico (para Bluetooth)
- NuGet adicionales (el proyecto los incluye):
  - `SkiaSharp` — rasterización de imágenes para ESC/POS
  - `QuestPDF` — generación de PDF (el cliente implementa `IRenderer`)

### UI

Pantalla principal con pestañas:
- **Preview** — vista previa del documento de multa en pantalla
- **ESC/POS** — hex dump de los comandos generados
- **PDF** — vista previa del PDF generado
- **API** — botón para exportar el ticket en Base64 a un endpoint REST

---

## Cómo ejecutar

```bash
# Ejemplo 01
cd samples/MotorDsl.SampleApp
dotnet build -f net10.0-android

# Ejemplo 02 (via ProjectReference)
cd samples/MotorDsl.MultaApp
dotnet build -f net10.0-android

# Ejemplo 03 (formato integrado)
cd samples/MotorDsl.Integrated.MultaApp
dotnet build -f net10.0-android

# Ejemplo 04 (via NuGet packages)
cd samples/MotorDsl.MultaApp.Nuget
dotnet build -f net10.0-android
```

> Ambos requieren un dispositivo Android conectado o emulador con soporte Bluetooth para pruebas de impresión.

---

## Ejemplo 03 — MotorDsl.Integrated.MultaApp

**Ubicación:** `samples/MotorDsl.Integrated.MultaApp/`

### Propósito

Misma acta de infracción que el Ejemplo 02, pero usando el **formato integrado** del DSL (`"format": "integrated"`). Demuestra la modalidad alternativa de entrada del motor: el JSON ya viene con todos los datos resueltos, sin `{{placeholders}}`, sin `loop` y sin `conditional`. El consumidor llama a `engine.Render(json, profile)` — sin diccionario de datos.

### Diferencia clave con MultaApp

| Aspecto                | MotorDsl.MultaApp                    | MotorDsl.Integrated.MultaApp                  |
|------------------------|--------------------------------------|-----------------------------------------------|
| Modalidad DSL          | Template + Data separados            | JSON integrado (`"format": "integrated"`)     |
| Templates registrados  | `MultaDsl` + `TicketSimpleDsl` + `ComprobanteDsl` | Único: `MultaIntegratedDsl.Document`          |
| Llamada al motor       | `engine.Render(template, data, profile)` | `engine.Render(integratedJson, profile)`  |
| Pipeline interno       | Parse → Validate → **Evaluate** → Layout → Render | Parse → Validate → Layout → Render            |
| ApplicationId          | `com.motordsl.multaapp`              | `com.motordsl.integrated.multaapp`            |
| Namespace              | `MotorDsl.MultaApp.*`                | `MotorDsl.Integrated.MultaApp.*`              |

### Qué demuestra

- Construcción de un JSON integrado completo desde C# (incluyendo logo y firma base64 embebidos)
- Loop de infracciones expandido manualmente como N `container` concretos
- Llamada al overload `IDocumentEngine.Render(string, DeviceProfile)` (sin `data`)
- Coexistencia con renderers custom y rasterizer (las mismas implementaciones que `MultaApp`: `BitmapEscPosRenderer`, `PdfRenderer`, `SkiaSharpRasterizer`)
- Validación específica del modo integrado (`TemplateValidator` rechaza placeholders residuales)

### Estructura de archivos

```
samples/MotorDsl.Integrated.MultaApp/
├── MotorDsl.Integrated.MultaApp.csproj
├── MauiProgram.cs
├── Templates/
│   └── MultaIntegratedDsl.cs       ← un único string `Document` con el JSON integrado
├── Pages/
│   └── MainPage.xaml / .xaml.cs    ← llama engine.Render(json, profile)
├── Renderers/                      ← idénticos a MultaApp (Project copy)
├── Services/
├── Controls/
├── Platforms/
└── Resources/
```

📄 Detalle: [`ejemplo-03-multa-integrada.md`](ejemplo-03-multa-integrada.md)

---

## Ejemplo 04 — MotorDsl.MultaApp.Nuget

**Ubicación:** `samples/MotorDsl.MultaApp.Nuget/`

### Propósito dual

1. **Test de integración end-to-end:** valida que los 4 paquetes publicados en NuGet.org funcionen correctamente en una app MAUI real.
2. **Ejemplo para el usuario final:** demuestra la forma canónica de integrar MotorDsl en un proyecto nuevo, sin necesidad de clonar el repositorio.

### Diferencia clave con MultaApp

| Aspecto              | MotorDsl.MultaApp         | MotorDsl.MultaApp.Nuget          |
|----------------------|---------------------------|----------------------------------|
| Referencias motor    | `<ProjectReference>`      | `<PackageReference>` v1.0.2      |
| ApplicationId        | com.motordsl.multaapp     | com.motordsl.multaapp.nuget      |
| Namespace            | MotorDsl.MultaApp.*       | MotorDsl.MultaApp.Nuget.*        |
| Propósito principal  | Desarrollo del motor      | Consumidor final / integración   |

### Referencias NuGet usadas

```xml
<PackageReference Include="MotorDsl.Core"       Version="1.0.2" />
<PackageReference Include="MotorDsl.Parser"     Version="1.0.2" />
<PackageReference Include="MotorDsl.Rendering"  Version="1.0.2" />
<PackageReference Include="MotorDsl.Extensions" Version="1.0.2" />
```

### Qué demuestra

- Instalación de MotorDsl desde NuGet.org (`Install-Package MotorDsl.Extensions`)
- Configuración DI con `AddMotorDslEngine()` desde un NuGet
- Renderers custom (`BitmapEscPosRenderer`, `PdfRenderer`) implementados por la app (no incluidos en el NuGet)
- `IBitmapRasterizer` implementado con SkiaSharp localmente
- Funcionalidad idéntica a MultaApp: impresión BT (Android), PDF, preview, iOS stub

### Estructura de archivos

```
samples/MotorDsl.MultaApp.Nuget/
├── MotorDsl.MultaApp.Nuget.csproj  ← PackageReference en lugar de ProjectReference
├── MauiProgram.cs
├── Templates/   (MultaDsl, TicketSimpleDsl, ComprobanteDsl)
├── Pages/       (MainPage)
├── Renderers/   (BitmapEscPosRenderer, PdfRenderer, SkiaSharpRasterizer)
├── Services/    (ThermalPrinterService, IThermalPrinterService, PrinterProfile)
├── Controls/    (MauiDocumentPreview)
└── Platforms/   (Android + iOS)
```

## Relación con la documentación

| Documento                                                  | Relación                        |
|------------------------------------------------------------|---------------------------------|
| `docs/10_developer_guide/guia-integracion-maui.md`         | Setup inicial del motor en MAUI |
| `docs/10_developer_guide/formato-dsl-templates.md`         | Sintaxis del DSL                |
| `docs/10_developer_guide/formato-perfiles-impresora.md`    | Configuración de DeviceProfile  |
| `docs/10_developer_guide/integracion-api-rest.md`          | Patrón REST con ToBase64()      |
| `docs/11_examples/ejemplo-01-simple.md`                    | Detalle del Ejemplo 01          |
| `docs/11_examples/ejemplo-02-multa.md`                     | Detalle del Ejemplo 02          |
