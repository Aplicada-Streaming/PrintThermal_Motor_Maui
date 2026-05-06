# Ejemplos de Uso — MotorDsl

> Aplicaciones de ejemplo que demuestran cómo integrar la librería MotorDsl en
> proyectos .NET MAUI sobre los 7 paquetes publicados.

---

## Índice de Ejemplos

| #  | Proyecto                              | Nivel     | Descripción                                                                 |
|----|---------------------------------------|-----------|-----------------------------------------------------------------------------|
| 01 | MotorDsl.SampleApp                    | Básico    | Ticket simple — aprender la librería paso a paso                            |
| 02 | MotorDsl.MultaApp                     | Avanzado  | Multa de tránsito (template + datos) — todas las funcionalidades clásicas  |
| 03 | MotorDsl.Integrated.MultaApp          | Avanzado  | Multa con formato integrado (JSON pre-resuelto)                             |
| 04 | MotorDsl.Nuget.MultaApp               | Avanzado  | Idem MultaApp — consume los paquetes NuGet publicados                       |
| 05 | MotorDsl.Nuget.Integrated.MultaApp    | Avanzado  | Idem Integrated — consume los paquetes NuGet publicados                     |

---

## Paquetes consumidos

Los samples del repositorio se apoyan en los **7 paquetes** del Motor:

| Paquete | Rol |
|---|---|
| `MotorDsl.Core` | Núcleo: contratos, modelos, evaluador, layout. |
| `MotorDsl.Parser` | Parser DSL JSON → AST. |
| `MotorDsl.Rendering` | Renderers texto + ESC/POS básicos. |
| `MotorDsl.Extensions` | Fluent DI (`AddMotorDslEngine`). |
| `MotorDsl.Printing.Abstractions` | Contratos de transporte y orquestador. |
| `MotorDsl.Bluetooth` | Transport BT Classic SPP (Android). |
| `MotorDsl.Maui` | Controles MAUI + renderers PDF/Raster/Bitmap-EscPos + error handler. |

> Recomendación de consumo: **`Install-Package MotorDsl.Maui`** (que trae las
> demás transitivamente) **+ `Install-Package MotorDsl.Bluetooth`** para el
> transport en Android.

---

## Ejemplo 01 — MotorDsl.SampleApp

**Ubicación:** `samples/MotorDsl.SampleApp/`

### Qué demuestra

- Configuración mínima con `AddMotorDslEngine()`.
- Template DSL básico con bindings (`{{campo}}`).
- Renderizado a texto plano y ESC/POS.
- Servicios locales de impresión BT (heredados; los nuevos samples usan el orquestador del paquete).

### UI

Pantalla única con escaneo, lista de dispositivos, botón Imprimir y hex dump.

---

## Ejemplo 02 — MotorDsl.MultaApp

**Ubicación:** `samples/MotorDsl.MultaApp/`

### Qué demuestra

- Template complejo (text, container, loop, conditional, image, table).
- Logo base64 embebido.
- Loop de infracciones con tabla.
- Código QR de pago.
- Validadores formales (`ITemplateValidator`, `IDataValidator`, `IProfileValidator`).
- Preview MAUI, ESC/POS bitmap, PDF (renderers locales).
- Hex dump, exportación Base64.

### UI

Pestañas Preview / ESC/POS / PDF / API.

---

## Ejemplo 03 — MotorDsl.Integrated.MultaApp

**Ubicación:** `samples/MotorDsl.Integrated.MultaApp/`

### Propósito

Misma acta de infracción que el Ejemplo 02 pero usando el **formato integrado**
del DSL (`"format": "integrated"`). El JSON ya viene con todos los datos
resueltos: sin `{{placeholders}}`, sin `loop`, sin `conditional`. El consumidor
llama `engine.Render(json, profile)` — sin diccionario de datos.

### Diferencia clave con MultaApp

| Aspecto | MotorDsl.MultaApp | MotorDsl.Integrated.MultaApp |
|---|---|---|
| Modalidad DSL | Template + Data separados | JSON integrado |
| Llamada al motor | `Render(template, data, profile)` | `Render(integratedJson, profile)` |
| Pipeline interno | Parse → Validate → **Evaluate** → Layout → Render | Parse → Validate → Layout → Render |
| ApplicationId | `com.motordsl.multaapp` | `com.motordsl.integrated.multaapp` |

📄 Detalle: [`ejemplo-03-multa-integrada.md`](ejemplo-03-multa-integrada.md)

---

## Ejemplo 04 — MotorDsl.Nuget.MultaApp

**Ubicación:** `samples/MotorDsl.Nuget.MultaApp/`

### Propósito dual

1. **Test de integración end-to-end:** valida que los paquetes publicados en
   nuget.org funcionen correctamente en una app MAUI real.
2. **Ejemplo para el usuario final:** demuestra la forma canónica de integrar
   MotorDsl en un proyecto nuevo, sin clonar el repositorio.

### Diferencias clave

- Reemplaza `<ProjectReference>` locales por `<PackageReference>`.
- **Ya no tiene** carpetas `Services/`, `Renderers/`, `Controls/` propias —
  todos esos artefactos ahora viven en `MotorDsl.Maui` y `MotorDsl.Bluetooth`.
- Bindea controles MAUI directamente al `IThermalPrinterService` resuelto del DI.

📄 Detalle: [`ejemplo-03-multaapp-nuget.md`](ejemplo-03-multaapp-nuget.md)

---

## Ejemplo 05 — MotorDsl.Nuget.Integrated.MultaApp

**Ubicación:** `samples/MotorDsl.Nuget.Integrated.MultaApp/`

### Propósito

Combinación de **NuGet** + **formato integrado**. Equivalente al Ejemplo 03
pero consumiendo los paquetes y los controles MAUI desde NuGet.

### Estructura

Comparte el mismo template integrado (`MultaIntegratedDsl.Document`) más
`TicketSimpleIntegratedDsl` y `ComprobanteIntegratedDsl`. La página principal
permite seleccionar entre los 3 documentos y previsualizar / imprimir / abrir
PDF.

---

## Cómo ejecutar

```bash
# Ejemplo 01
dotnet build -t:Run -f net10.0-android samples/MotorDsl.SampleApp/MotorDsl.SampleApp.csproj

# Ejemplo 02
dotnet build -t:Run -f net10.0-android samples/MotorDsl.MultaApp/MotorDsl.MultaApp.csproj

# Ejemplo 03 (integrado)
dotnet build -t:Run -f net10.0-android samples/MotorDsl.Integrated.MultaApp/MotorDsl.Integrated.MultaApp.csproj

# Ejemplo 04 (NuGet)
dotnet build -t:Run -f net10.0-android samples/MotorDsl.Nuget.MultaApp/MotorDsl.Nuget.MultaApp.csproj

# Ejemplo 05 (NuGet + integrado)
dotnet build -t:Run -f net10.0-android samples/MotorDsl.Nuget.Integrated.MultaApp/MotorDsl.Nuget.Integrated.MultaApp.csproj
```

> Atajos en `scripts/local/run-*.bat` y publicación APK en `scripts/mobile/publish-*-apk.bat`.

---

## Relación con la documentación

| Documento | Relación |
|---|---|
| `docs/10_developer_guide/guia-integracion-maui.md` | Setup inicial del motor en MAUI. |
| `docs/10_developer_guide/componentes-ux-maui.md` | Referencia de los controles `muic:*`. |
| `docs/10_developer_guide/render-pixelado-y-pdf.md` | Renderers PDF / Raster / QR. |
| `docs/10_developer_guide/transports-y-extensibilidad.md` | Implementar transports custom (USB, Red, BLE). |
| `docs/10_developer_guide/formato-dsl-templates.md` | Sintaxis del DSL. |
| `docs/10_developer_guide/formato-perfiles-impresora.md` | Configuración de DeviceProfile. |
| `docs/11_examples/ejemplo-01-simple.md` | Detalle del Ejemplo 01. |
| `docs/11_examples/ejemplo-02-multa.md` | Detalle del Ejemplo 02. |
| `docs/11_examples/ejemplo-03-multa-integrada.md` | Detalle del Ejemplo 03. |
| `docs/11_examples/ejemplo-03-multaapp-nuget.md` | Detalle del Ejemplo 04. |
