@echo off
setlocal enabledelayedexpansion
echo ===================================================
echo  MotorDsl - Build, test y publicacion en nuget.org
echo ===================================================
echo.

:: -------------------------------------------------------
:: [0/6] Resolver API Key (env var o prompt interactivo)
:: -------------------------------------------------------
if "%MOTORDSL_NUGET_API_KEY%"=="" (
    echo.
    echo ADVERTENCIA: La API key se mostrara en pantalla al escribirla.
    echo Para mayor seguridad, setear MOTORDSL_NUGET_API_KEY antes de ejecutar el script.
    echo Generar una key en: https://www.nuget.org/account/apikeys
    echo.
    set /p MOTORDSL_NUGET_API_KEY="NuGet.org API Key: "
    if "!MOTORDSL_NUGET_API_KEY!"=="" (
        echo ERROR: La API key es obligatoria. Abortando.
        exit /b 1
    )
)

set NUGET_SOURCE=https://api.nuget.org/v3/index.json

:: -------------------------------------------------------
:: Calcular siguiente version consultando nuget.org
:: (auto-bump del patch a partir de la ultima version publicada de cada paquete)
:: -------------------------------------------------------
echo Consultando ultimas versiones publicadas en nuget.org ...

for /f "delims=" %%i in ('powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0get-next-version.ps1" -PackageName "MotorDsl.Core"') do set CORE_NEXT=%%i
for /f "delims=" %%i in ('powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0get-next-version.ps1" -PackageName "MotorDsl.Parser"') do set PARSER_NEXT=%%i
for /f "delims=" %%i in ('powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0get-next-version.ps1" -PackageName "MotorDsl.Rendering"') do set RENDERING_NEXT=%%i
for /f "delims=" %%i in ('powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0get-next-version.ps1" -PackageName "MotorDsl.Extensions"') do set EXTENSIONS_NEXT=%%i

if "%CORE_NEXT%"=="" ( echo ERROR: No se pudo calcular version de MotorDsl.Core. & exit /b 1 )
if "%PARSER_NEXT%"=="" ( echo ERROR: No se pudo calcular version de MotorDsl.Parser. & exit /b 1 )
if "%RENDERING_NEXT%"=="" ( echo ERROR: No se pudo calcular version de MotorDsl.Rendering. & exit /b 1 )
if "%EXTENSIONS_NEXT%"=="" ( echo ERROR: No se pudo calcular version de MotorDsl.Extensions. & exit /b 1 )

:: Version unificada = max(Core, Parser, Rendering, Extensions) para mantener los 4 paquetes alineados
set MOTORDSL_VERSION=%CORE_NEXT%
for %%V in (%PARSER_NEXT% %RENDERING_NEXT% %EXTENSIONS_NEXT%) do (
    for /f "delims=" %%M in ('powershell -NoProfile -Command "if ([version]'%%V' -gt [version]'!MOTORDSL_VERSION!') { '%%V' } else { '!MOTORDSL_VERSION!' }"') do set MOTORDSL_VERSION=%%M
)

if "!MOTORDSL_VERSION!"=="" (
    echo ERROR: No se pudo calcular la version unificada. Abortando.
    exit /b 1
)

echo.
echo  Configuracion:
echo    Fuente:            %NUGET_SOURCE%
echo    API Key:           [oculta]
echo    Version unificada: !MOTORDSL_VERSION!
echo      ^(Core next=%CORE_NEXT%, Parser next=%PARSER_NEXT%, Rendering next=%RENDERING_NEXT%, Extensions next=%EXTENSIONS_NEXT%^)
echo.
echo    Los 4 paquetes se publicaran con la misma version para evitar NU1605.
echo.

set SRC_DIR=%~dp0..\..\src
set NUPKG_DIR=%~dp0..\..\nupkg

if not exist "%NUPKG_DIR%" mkdir "%NUPKG_DIR%"

:: -------------------------------------------------------
:: [1/6] Restore de las 4 librerias
:: -------------------------------------------------------
echo [1/6] Restore de las 4 librerias ...
for %%P in (Core Parser Rendering Extensions) do (
    dotnet restore "%SRC_DIR%\MotorDsl.%%P\MotorDsl.%%P.csproj" --nologo
    if !errorlevel! neq 0 (
        echo ERROR: Fallo restore de MotorDsl.%%P. Abortando.
        exit /b 1
    )
)
echo.

:: -------------------------------------------------------
:: [2/6] Build Release de las 4 librerias
:: -------------------------------------------------------
echo [2/6] Build Release de las 4 librerias ...
for %%P in (Core Parser Rendering Extensions) do (
    dotnet build "%SRC_DIR%\MotorDsl.%%P\MotorDsl.%%P.csproj" -c Release --no-restore --nologo /p:Version=%MOTORDSL_VERSION%
    if !errorlevel! neq 0 (
        echo ERROR: Fallo build de MotorDsl.%%P. Abortando.
        exit /b 1
    )
)
echo.

:: -------------------------------------------------------
:: [3/6] Tests
:: -------------------------------------------------------
echo [3/6] Ejecutando tests ...
dotnet restore "%SRC_DIR%\MotorDsl.Tests\MotorDsl.Tests.csproj" --nologo
dotnet test "%SRC_DIR%\MotorDsl.Tests\MotorDsl.Tests.csproj" -c Release --nologo
if %errorlevel% neq 0 (
    echo ERROR: Fallaron los tests. Abortando publicacion.
    exit /b 1
)
echo.

:: -------------------------------------------------------
:: [4/6] Pack de las 4 librerias
:: -------------------------------------------------------
echo [4/6] Empaquetando las 4 librerias ...
for %%P in (Core Parser Rendering Extensions) do (
    dotnet pack "%SRC_DIR%\MotorDsl.%%P\MotorDsl.%%P.csproj" -c Release --no-build --nologo -p:PackageVersion=%MOTORDSL_VERSION% -p:PackageReleaseNotes="Release %MOTORDSL_VERSION%" -o "%NUPKG_DIR%"
    if !errorlevel! neq 0 (
        echo ERROR: Fallo pack de MotorDsl.%%P. Abortando.
        exit /b 1
    )
)
echo.

echo Paquetes generados en %NUPKG_DIR%:
dir /b "%NUPKG_DIR%\MotorDsl.*.%MOTORDSL_VERSION%.nupkg"
echo.

:: -------------------------------------------------------
:: [5/6] Push a nuget.org (orden: Core -> Parser/Rendering -> Extensions)
:: -------------------------------------------------------
echo [5/6] Publicando en nuget.org ...
for %%P in (Core Parser Rendering Extensions) do (
    set NUPKG=%NUPKG_DIR%\MotorDsl.%%P.%MOTORDSL_VERSION%.nupkg
    if not exist "!NUPKG!" (
        echo ERROR: No se encontro !NUPKG!
        exit /b 1
    )
    echo       Publicando MotorDsl.%%P.%MOTORDSL_VERSION%.nupkg ...
    dotnet nuget push "!NUPKG!" --source "%NUGET_SOURCE%" --api-key "%MOTORDSL_NUGET_API_KEY%" --skip-duplicate
    set PUSH_CODE=!errorlevel!
    if !PUSH_CODE!==0 (
        echo       MotorDsl.%%P publicado correctamente.
    ) else (
        echo ADVERTENCIA: push de MotorDsl.%%P termino con codigo !PUSH_CODE! ^(409 = version ya existente en el feed^).
    )
)
echo.

:: -------------------------------------------------------
:: [6/6] Limpieza y resumen
:: -------------------------------------------------------
echo [6/6] Limpiando cache HTTP de NuGet ...
dotnet nuget locals http-cache --clear
echo.

echo ===================================================
echo  Publicacion completada en nuget.org
echo    MotorDsl.Core        %MOTORDSL_VERSION%
echo    MotorDsl.Parser      %MOTORDSL_VERSION%
echo    MotorDsl.Rendering   %MOTORDSL_VERSION%
echo    MotorDsl.Extensions  %MOTORDSL_VERSION%
echo.
echo  La indexacion en nuget.org puede demorar varios minutos.
echo ===================================================
echo.
echo Presiona cualquier tecla para cerrar esta ventana...
pause > nul
