@echo off
setlocal
cd /d "%~dp0"

echo Publicando todas las apps Android...

rem call update-packages.bat
call ./publish-MotorDsl.Integrated.MultaApp-apk.bat
call ./publish-MotorDsl.MultaApp-apk.bat
call ./publish-MotorDsl.Nuget.Integrated.MultaApp-apk.bat
call ./publish-MotorDsl.Nuget.MultaApp-apk.bat
call ./publish-MotorDsl.SampleApp-apk.bat

endlocal

