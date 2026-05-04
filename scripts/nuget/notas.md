# Publicacion de paquetes MotorDsl en nuget.org

Script: `publish-motordsl-nuget.bat`

## Que hace

1. Resuelve la API key de nuget.org desde `MOTORDSL_NUGET_API_KEY` (env var) o la pide por prompt.
2. Consulta `https://api.nuget.org/v3-flatcontainer/<paquete>/index.json` para los 4 paquetes
   (`MotorDsl.Core`, `MotorDsl.Parser`, `MotorDsl.Rendering`, `MotorDsl.Extensions`) y calcula
   `version unificada = max(next(patch) de cada paquete)`. Esto evita NU1605 al consumir.
3. Restore + Build Release de las 4 librerias con `/p:Version=<unificada>`.
4. Restore + `dotnet test` de `MotorDsl.Tests`. Si fallan, aborta antes de publicar.
5. `dotnet pack` de las 4 librerias con `-p:PackageVersion=<unificada>` a `./nupkg/`.
6. `dotnet nuget push` a `https://api.nuget.org/v3/index.json` con `--skip-duplicate`
   (orden: Core -> Parser -> Rendering -> Extensions).
7. Limpia el cache HTTP de NuGet.

## Requisitos

- .NET SDK 10 instalado (`dotnet --version`).
- API key de nuget.org con scope de push: https://www.nuget.org/account/apikeys
- Conexion a internet (para consultar versiones publicadas y para el push).

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

El script implementa la opcion **auto-bump de patch**: lee la ultima version publicada en
nuget.org de cada paquete e incrementa el patch (`1.0.2` -> `1.0.3`). No reescribe los
`.csproj`; la version se inyecta en build/pack via `/p:Version` y `-p:PackageVersion`.

Otras opciones disponibles si en el futuro se quiere cambiar el esquema:

- **Manual**: declarar `<Version>X.Y.Z</Version>` en cada `.csproj` y publicar tal cual.
  Si se republica sin tocarlo, el feed devuelve 409 (`--skip-duplicate` lo absorbe como
  ADVERTENCIA y no sube nada nuevo).
- **Prerelease por timestamp/commit**: `Version = 1.0.0-pre.20260504.1430` o
  `-ci.<shortsha>`. Se inyecta con `/p:VersionSuffix=...` sin tocar el `.csproj`. Cada push
  genera un paquete nuevo y no afecta la version estable.
- **MinVer / Nerdbank.GitVersioning**: versionado derivado de tags de git. Mas robusto,
  requiere agregar un `PackageReference` a cada proyecto y tagear releases.

## Notas

- Si un paquete aun no existe en nuget.org, `get-next-version.ps1` devuelve el fallback `1.0.0`.
- La indexacion en nuget.org tras un push puede tardar varios minutos.
- El workflow `.github/workflows/cd-nuget.yml` hace lo equivalente desde CI usando
  `secrets.NUGET_API_KEY`. Este script local sirve para publicaciones puntuales sin pasar
  por GitHub Actions.
