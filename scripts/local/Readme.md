# Scripts locales — build y run en Android

Atajos `.bat` para compilar y lanzar los samples MAUI en un dispositivo
Android conectado por ADB. Equivale a `dotnet build -t:Run -f net10.0-android`
con un check previo de presencia de dispositivo.

---

## 📋 Catálogo

| Script | Sample que lanza |
|---|---|
| `run-MotorDsl.SampleApp.bat` | `samples/MotorDsl.SampleApp/` |
| `run-MotorDsl.MultaApp.bat` | `samples/MotorDsl.MultaApp/` |
| `run-MotorDsl.Integrated.MultaApp.bat` | `samples/MotorDsl.Integrated.MultaApp/` |
| `run-MotorDsl.Nuget.MultaApp.bat` | `samples/MotorDsl.Nuget.MultaApp/` |
| `run-MotorDsl.Nuget.Integrated.MultaApp.bat` | `samples/MotorDsl.Nuget.Integrated.MultaApp/` |
| `run-All.bat` | Lanza los 5 samples en cadena (también corre `update-packages.bat` antes). |
| `update-packages.bat` | Actualiza las `<PackageReference>` de los samples Nuget a la última versión estable de `MotorDsl.*` publicada en nuget.org (usa `dotnet-outdated`). |

---

## 🚀 Uso típico

```bat
cd scripts\local
run-MotorDsl.Nuget.Integrated.MultaApp.bat
```

Cada script de run hace:

1. Agrega `C:\Program Files (x86)\Android\android-sdk\platform-tools` al PATH.
2. Verifica con `adb devices` que haya al menos un dispositivo en estado
   `device` (no `unauthorized` ni `offline`).
3. Si no hay dispositivo: muestra ayuda y sale con `errorlevel 1`.
4. Si hay dispositivo: ejecuta
   `dotnet build "<sample>.csproj" -t:Run -f net10.0-android`.

---

## 🔄 update-packages.bat

Útil **después** de publicar paquetes nuevos en nuget.org:

```bat
update-packages.bat
```

1. Si no está `dotnet-outdated-tool`, lo instala globalmente.
2. Lista los paquetes desactualizados de los 2 samples Nuget.
3. Hace `--upgrade --include MotorDsl` para subir los 4 paquetes core
   (`Core`, `Parser`, `Rendering`, `Extensions`) a la última versión.
4. Vuelve a listar para verificar.

> **Limitación actual:** `update-packages.bat` upgradea sólo los 4 paquetes
> originales. Los 3 nuevos (`MotorDsl.Maui`, `MotorDsl.Bluetooth`,
> `MotorDsl.Printing.Abstractions`) hoy se consumen vía `<ProjectReference>`
> en los samples (Fase 1). Cuando se publique la primera versión, agregar
> esos paquetes al filtro `--include`.

---

## 🐛 Troubleshooting

- **"No hay dispositivo Android conectado"**: conectar por USB con depuración
  habilitada y aceptar el prompt de autorización en el equipo. Validar con
  `adb devices`.
- **Falla `dotnet build` con "workload missing"**:
  `dotnet workload install maui android`.
- **Falla con "could not load library libQuestPdfSkia.so"**: ver
  `samples/notas-debug-android.md` (suele ser por una APK previa con
  `EmbedAssembliesIntoApk=false`; rebuild con `--no-incremental`).

---

## 📦 Generación de APK firmado

Para empaquetar APK y distribuirlo, usar `scripts/mobile/`:

```bat
cd scripts\mobile
publish-MotorDsl.Nuget.Integrated.MultaApp-apk.bat
```

Más detalles en [`../mobile/Readme.md`](../mobile/Readme.md).
