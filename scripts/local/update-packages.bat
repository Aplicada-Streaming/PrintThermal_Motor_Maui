@echo off
echo Actualizando paquetes NuGet en proyectos sample...

cd ..\..

where dotnet-outdated >nul 2>&1
if errorlevel 1 (
    echo Instalando dotnet-outdated-tool...
    dotnet tool install --global dotnet-outdated-tool
    if errorlevel 1 (
        echo [ERROR] No se pudo instalar dotnet-outdated-tool.
        pause
        exit /b 1
    )
)

set NUGET_PROJECT=samples\MotorDsl.Nuget.MultaApp\MotorDsl.Nuget.MultaApp.csproj
set NUGET_INT_PROJECT=samples\MotorDsl.Nuget.Integrated.MultaApp\MotorDsl.Nuget.Integrated.MultaApp.csproj

echo.
echo === Paquetes desactualizados (antes) ===
dotnet outdated "%NUGET_PROJECT%"
dotnet outdated "%NUGET_INT_PROJECT%"

echo.
echo === Actualizando paquetes MotorDsl.* a la ultima version estable ===
dotnet outdated "%NUGET_PROJECT%" --upgrade --include MotorDsl
dotnet outdated "%NUGET_INT_PROJECT%" --upgrade --include MotorDsl

echo.
echo === Verificacion final ===
dotnet outdated "%NUGET_PROJECT%"
dotnet outdated "%NUGET_INT_PROJECT%"

echo.
echo Listo. Revisa los .csproj y haz commit si los cambios son correctos.
pause
