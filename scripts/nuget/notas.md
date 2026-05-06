# Publicacion de paquetes MotorDsl en nuget.org

Script: `publish-motordsl-nuget.bat`

## Que hace

1. Resuelve la API key de nuget.org desde `MOTORDSL_NUGET_API_KEY` (env var)
   o la pide por prompt.
2. Consulta `https://api.nuget.org/v3-flatcontainer/<paquete>/index.json` para
   los **7 paquetes**:
   - `MotorDsl.Core`
   - `MotorDsl.Parser`
   - `MotorDsl.Rendering`
   - `MotorDsl.Extensions`
   - `MotorDsl.Printing.Abstractions`
   - `MotorDsl.Bluetooth`
   - `MotorDsl.Maui`

   Calcula `version unificada = max(next(patch) de cada paquete)`. Esto evita
   `NU1605` al consumir desde apps que mezclen los 7.
3. Restore + Build Release de las 7 librerias con
   `/p:Version=<unificada> /p:MotorDslVersion=<unificada>`.
4. Restore + `dotnet test` de `MotorDsl.Tests`. Si fallan, aborta antes de
   publicar.
5. `dotnet pack` de las 7 librerias con
   `-p:PackageVersion=<unificada> -p:MotorDslVersion=<unificada>`
   a `./nupkg/`.
6. `dotnet nuget push` a `https://api.nuget.org/v3/index.json` con
   `--skip-duplicate`.
   **Orden de push (por dependencias)**:
   `Printing.Abstractions` → `Core` → `Parser` → `Rendering` → `Extensions` →
   `Bluetooth` → `Maui`.
7. Tag git `v<unificada>` y push a origin (dispara el workflow
   `cd-nuget.yml`).
8. Limpia el cache HTTP de NuGet.

## Variable `MotorDslVersion`

El script pasa `-p:MotorDslVersion=<unificada>` adicional a `Version` y
`PackageVersion`. Esto permite que los `.csproj` (e.g. `MotorDsl.Bluetooth`)
referencien internamente esa versión cuando declaren PackageReferences entre
ellos en escenarios futuros sin tener que parsear `Version` en MSBuild.

## Requisitos

- .NET SDK 10 instalado (`dotnet --version`).
- API key de nuget.org con scope de push:
  https://www.nuget.org/account/apikeys
- Conexion a internet (para consultar versiones publicadas y para el push).
- Git configurado para push a origin si se quiere taggear.

## Uso

Recomendado (sin que la key quede en pantalla):

```bat
set MOTORDSL_NUGET_API_KEY=oy2x...tu-key...
scripts\nuget\publish-motordsl-nuget.bat
```

Interactivo (la key se ve al tipearla):

```bat
scripts\nuget\publish-motordsl-nuget.bat
```

## Estrategias de versionado (referencia)

El script implementa la opcion **auto-bump de patch**: lee la ultima version
publicada en nuget.org de cada paquete e incrementa el patch
(`1.0.4` -> `1.0.5`). No reescribe los `.csproj`; la version se inyecta en
build/pack via `/p:Version`, `-p:PackageVersion` y `-p:MotorDslVersion`.

Otras opciones disponibles si en el futuro se quiere cambiar el esquema:

- **Manual**: declarar `<Version>X.Y.Z</Version>` en cada `.csproj` y publicar
  tal cual. Si se republica sin tocarlo, el feed devuelve 409
  (`--skip-duplicate` lo absorbe como ADVERTENCIA y no sube nada nuevo).
- **Prerelease por timestamp/commit**:
  `Version = 1.0.0-pre.20260506.1430` o `-ci.<shortsha>`. Se inyecta con
  `/p:VersionSuffix=...` sin tocar el `.csproj`. Cada push genera un paquete
  nuevo y no afecta la version estable.
- **MinVer / Nerdbank.GitVersioning**: versionado derivado de tags de git.
  Mas robusto, requiere agregar un `PackageReference` a cada proyecto y
  taggear releases.

## Notas

- Si un paquete aun no existe en nuget.org, `get-next-version.ps1` devuelve
  el fallback `1.0.0`.
- La indexacion en nuget.org tras un push puede tardar varios minutos
  (especialmente para los paquetes nuevos en su primera publicación).
- Los 7 paquetes se publican con la **misma version unificada** para evitar
  resoluciones cruzadas con `NU1605`.
- El workflow `.github/workflows/cd-nuget.yml` hace lo equivalente desde CI
  usando `secrets.NUGET_API_KEY`. Este script local sirve para publicaciones
  puntuales sin pasar por GitHub Actions.
- `MotorDsl.Bluetooth` y `MotorDsl.Maui` tienen TFMs duales
  (`net10.0-android;net10.0-ios`). El pack genera ambos targets dentro del
  mismo `.nupkg`. Para iOS, el contenido es estructural: muchas operaciones
  lanzan `PlatformNotSupportedException` en runtime.
