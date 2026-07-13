# Changelog

Todos los cambios notables de **MotorDsl** se documentan en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/)
y el proyecto sigue [Versionado Semántico](https://semver.org/lang/es/). La
versión de los paquetes se inyecta en build/pack vía `-p:PackageVersion` /
`-p:MotorDslVersion` (ver `docs/09_devops/estrategia-versionado_v1.0.md`), por lo
que el número de versión **no** vive en los `.csproj`.

## [1.0.13] - 2026-07-13

### Corregido

- **Rasterizado de imágenes bitmap devolvía el ticket completo en 0 bytes.**
  `SkiaSharpRasterizer` decodificaba con `SKBitmap.Decode(byte[])`, que en
  SkiaSharp 3.x devuelve `null` incluso para PNGs perfectamente válidos
  (reproducido con un PNG 1×1 estándar). Al devolver `null`, el rasterizador
  lanzaba `InvalidOperationException` y el `catch` de `BitmapEscPosRenderer`
  descartaba **todo** el documento (`Output = Array.Empty<byte>()`). Ahora la
  decodificación usa `SKImage.FromEncodedData` + `SKBitmap.FromImage`, que sí
  decodifica de forma fiable.
- **El estilo `bold` de los nodos de texto se ignoraba.** `LayoutEngine` sólo
  propagaba `align` al `DeviceMetadata`; los textos con `"bold": true` salían sin
  negrita. Ahora `bold` se propaga y los renderers lo aplican.

### Cambiado

- **`BitmapEscPosRenderer`: manejo de fallas de imagen por severidad.**
  - Una imagen que **no se puede rasterizar** ahora aborta el ticket con un
    `Error` **preciso** que incluye el `source` (recortado) y la causa
    (`Image rasterization failed (source='…'): <causa>`), en lugar del genérico
    `BitmapEscPos rendering failed`.
  - Una imagen que **decodifica pero sale en blanco** (típico de placeholders
    transparentes) genera un `Warning` y el ticket **sí** se imprime.
  - Se elimina la degradación silenciosa: los problemas de imagen siempre quedan
    visibles en `RenderResult.Errors` / `RenderResult.Warnings`.
- `SkiaSharpRasterizer`: el `Convert.FromBase64String` se envuelve para reportar
  un mensaje claro cuando el `source` no es base64 válido.

### Eliminado

- Trazas `Console.WriteLine` de depuración en `SkiaSharpRasterizer` y
  `BitmapEscPosRenderer` que volcaban parte del base64 de la imagen a la salida
  estándar en cada render.

[1.0.13]: https://github.com/Aplicada-Streaming/PrintThermal_Motor_Maui/releases/tag/v1.0.13
