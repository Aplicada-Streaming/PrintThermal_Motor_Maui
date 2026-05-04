@echo off
echo Lanzando MotorDsl.Nuget.MultaApp en dispositivo Android...

cd ..\..

set PROJECT_PATH=samples\MotorDsl.Nuget.MultaApp\MotorDsl.Nuget.MultaApp.csproj

set PATH=%PATH%;C:\Program Files (x86)\Android\android-sdk\platform-tools

set DEVICE_FOUND=
for /f "skip=1 tokens=1,2" %%a in ('adb devices') do (
    if "%%b"=="device" set DEVICE_FOUND=1
)

if not defined DEVICE_FOUND (
    echo.
    echo [ERROR] No hay ningun dispositivo Android conectado/autorizado.
    echo  - Conecta un telefono via USB con depuracion USB habilitada y autoriza la PC, o
    echo  - Inicia un emulador AVD desde Android Studio o con: emulator -avd ^<nombre^>
    echo.
    echo Estado actual de adb:
    adb devices
    echo.
    pause
    exit /b 1
)

echo.
adb devices
echo.

dotnet build "%PROJECT_PATH%" -t:Run -f net10.0-android

pause
