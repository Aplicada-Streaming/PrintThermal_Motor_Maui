# Cómo referenciar los `.nupkg` locales desde `/samples`

## Estado actual

Hoy los proyectos en [../samples/](../samples/) **no usan** los `.nupkg` locales — los traen de nuget.org. Lo confirma el comentario en [../samples/MotorDsl.Nuget.MultaApp/MotorDsl.Nuget.MultaApp.csproj](../samples/MotorDsl.Nuget.MultaApp/MotorDsl.Nuget.MultaApp.csproj):

```xml
<!-- NuGet packages (instead of ProjectReference) — validates published packages -->
<PackageReference Include="MotorDsl.Core" Version="1.0.3" />
```

`PackageReference` por sí solo **no apunta a una ruta** — busca el paquete en las "fuentes" (sources) configuradas. Si solo está nuget.org configurado, ignora la carpeta [./](./).

## Cómo hacer que use los `.nupkg` locales

NuGet resuelve paquetes en este orden: caché global del usuario → fuentes del `NuGet.config` más cercano → fuentes globales. Hay 3 caminos típicos:

### Opción 1 — `NuGet.config` en la raíz del repo (la más limpia)

Crear `NuGet.config` en la raíz (junto al `.sln`):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-nupkg" value="./nupkg" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

- `<clear />` evita que se hereden fuentes globales.
- La ruta `./nupkg` es **relativa al `NuGet.config`**.
- MSBuild "sube" desde cada `.csproj` buscando `NuGet.config`, así que cualquier proyecto en `samples/` lo encuentra.
- Los `<PackageReference>` siguen igual — solo cambia de dónde se resuelven.

Con esto, `dotnet restore` toma `MotorDsl.Core.1.0.3.nupkg` directamente de esta carpeta.

### Opción 2 — Agregar la fuente al usuario (global)

```powershell
dotnet nuget add source "E:\repos\...\nupkg" --name local-motordsl
```

Funciona, pero "ensucia" la config global del usuario y no es portable entre máquinas.

### Opción 3 — Carpeta directa en `<RestoreSources>` del `.csproj`

```xml
<PropertyGroup>
  <RestoreSources>$(RestoreSources);../../nupkg;https://api.nuget.org/v3/index.json</RestoreSources>
</PropertyGroup>
```

Funciona pero hay que repetirlo en cada `.csproj` — peor que la Opción 1.

## Detalle importante: caché global

Una vez que NuGet resolvió `MotorDsl.Core 1.0.3` desde nuget.org, queda cacheado en `%userprofile%\.nuget\packages\motordsl.core\1.0.3\`. Si después agregás la fuente local **con la misma versión**, NuGet usa el del caché y **no** vuelve a leer el `.nupkg` local. Para forzar que tome el local hay que:

```powershell
dotnet nuget locals all --clear
```

…o subir la versión del `.nupkg` (p. ej. `1.0.3-local`) para que sea distinta de la cacheada.

## Recomendación

Para este repo, **Opción 1**: un `NuGet.config` en la raíz con `./nupkg` antes que `nuget.org`. Así los samples `Nuget.*` se pueden probar contra paquetes recién empacados (paso `[4/7]` del script de publicación) **antes** de hacer push a nuget.org.
